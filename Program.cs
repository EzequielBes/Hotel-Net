// ============================================================
//  COMPOSIÇÃO DA APLICAÇÃO (Composition Root)
//  Este é o único lugar onde todas as camadas se conhecem.
//  Domain e Application NÃO referenciam Infrastructure diretamente.
// ============================================================

using CheckInApp.Application.UseCases.Hospedagem;
using CheckInApp.Application.UseCases.Reservas;
using CheckInApp.Application.UseCases.Identidade;
using CheckInApp.Infrastructure.Persistence;
using CheckInApp.Infrastructure.Persistence.Repositories;
using CheckInApp.Infrastructure.Security;
using CheckInApp.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Infraestrutura de Persistência ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=banco.db"));

// --- Adaptadores de Saída (Driven Adapters): Interface do Domain ← Implementação da Infra ---
builder.Services.AddScoped<IQuartoRepository, QuartoRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// --- Infraestrutura de Segurança ---
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// --- Casos de Uso (Application Layer) ---
builder.Services.AddScoped<ICheckInUseCase, CheckInUseCase>();
builder.Services.AddScoped<ICheckOutUseCase, CheckOutUseCase>();
builder.Services.AddScoped<IListarReservasUseCase, ListarReservasUseCase>();
builder.Services.AddScoped<IListarReservaCpfUseCase, ListarReservaCpfUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<ISignupUseCase, SignupUseCase>();

// --- Adaptadores de Entrada (Driving Adapters): Controllers ---
builder.Services.AddControllers();

// --- Autenticação JWT ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define o esquema de segurança JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Cole o token JWT aqui. Exemplo: eyJhbGci..."
    });

    // Aplica o esquema JWT em todos os endpoints automaticamente
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
