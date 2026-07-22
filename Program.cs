// ============================================================
//  APPLICATION COMPOSITION ROOT
//  This is the only place where all layers know each other.
//  Domain and Application do NOT reference Infrastructure directly.
// ============================================================

using CheckInApp.Application.UseCases.Hospitality;
using CheckInApp.Application.UseCases.Reservations;
using CheckInApp.Application.UseCases.Identity;
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Infrastructure.Persistence;
using CheckInApp.Infrastructure.Persistence.Repositories;
using CheckInApp.Infrastructure.Security;
using CheckInApp.Infrastructure.Messaging;
using CheckInApp.Infrastructure.Webhooks;
using CheckInApp.Domain.Ports;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence Infrastructure ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=hotel.db"));

// --- Output Adapters (Driven Adapters): Domain Interface ← Infrastructure Implementation ---
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoomCategoryRepository, RoomCategoryRepository>();
builder.Services.AddScoped<IRatePlanRepository, RatePlanRepository>();
builder.Services.AddScoped<IBookingOrderRepository, BookingOrderRepository>();
builder.Services.AddScoped<IBookingMessagePublisher, MassTransitBookingMessagePublisher>();
builder.Services.AddHttpClient<IWebhookSender, HttpWebhookSender>();

// --- Security Infrastructure ---
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// --- Use Cases (Application Layer) ---
builder.Services.AddScoped<ICheckInUseCase, CheckInUseCase>();
builder.Services.AddScoped<ICheckOutUseCase, CheckOutUseCase>();
builder.Services.AddScoped<IListReservationsUseCase, ListReservationsUseCase>();
builder.Services.AddScoped<IListReservationByCpfUseCase, ListReservationByCpfUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<ISignupUseCase, SignupUseCase>();
builder.Services.AddScoped<IListAvailableRoomsUseCase, ListAvailableRoomsUseCase>();
builder.Services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();
builder.Services.AddScoped<IGetBookingUseCase, GetBookingUseCase>();
builder.Services.AddScoped<IProcessBookingUseCase, ProcessBookingUseCase>();

// --- Messaging Infrastructure (RabbitMQ via MassTransit) ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProcessBookingConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });

        cfg.ReceiveEndpoint("process-booking", e =>
        {
            e.ConfigureConsumer<ProcessBookingConsumer>(context);

            // IMPORTANT: this partitioner is a correctness requirement, not just a
            // throughput optimization. BookingOrderRepository.TryAssignRoom wraps its
            // free-room lookup + assignment + SaveChanges in a Serializable transaction,
            // but Sqlite does not provide true multi-connection Serializable snapshot
            // isolation the way a server RDBMS does — under real concurrent contention
            // across connections it can throw locking/SQLITE_BUSY errors instead of
            // cleanly serializing two overlapping transactions. Double-booking is only
            // actually prevented here because keying by RoomCategoryId guarantees that
            // two TryAssignRoom calls for the same category never execute concurrently
            // in the first place. Do not remove this partitioner or raise per-partition
            // concurrency above 1 without first replacing TryAssignRoom's concurrency
            // control with something that is safe across real concurrent Sqlite
            // connections (e.g. a unique constraint-based assignment or a different DB).
            var partitioner = e.CreatePartitioner(8);
            e.UsePartitioner<ProcessBookingMessage>(partitioner, m => new Guid(m.Message.RoomCategoryId, 0, 0, new byte[8]));
        });
    });
});

// --- Input Adapters (Driving Adapters): Controllers ---
builder.Services.AddControllers();

// --- JWT Authentication ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Disable automatic claim type remapping so ClaimTypes.Role
    // is preserved exactly as written in the token, allowing
    // [Authorize(Roles = "...")] to match correctly.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        // Tell the framework which claim holds the role value
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // JWT security scheme for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the JWT token here. Example: eyJhbGci..."
    });

    // Apply JWT scheme to all endpoints automatically
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Global Exception Handling ---
// No custom exception types exist in this codebase yet: use cases throw bare
// System.Exception with a descriptive message (e.g. "Room category not found",
// "Booking order not found", "Guest count exceeds category capacity"). Without
// custom exception types we can't cleanly distinguish "not found" from
// "validation failed" by type, so this uses a pragmatic heuristic: any message
// containing "not found" maps to 404, everything else maps to 400. This is a
// stopgap that keeps unhandled domain exceptions from surfacing as raw 500s
// across all controllers (Booking, Hospitality, Auth) without requiring a
// larger refactor into typed exceptions.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = exceptionFeature?.Error.Message ?? "An unexpected error occurred";

        context.Response.StatusCode = message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
