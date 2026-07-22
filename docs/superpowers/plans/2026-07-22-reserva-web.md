# Reserva Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a self-service "web booking" flow (list available room categories, place an idempotent booking order, process it through RabbitMQ so the same room is never double-booked, notify an external webhook on confirm/reject) alongside the existing presencial CheckIn/CheckOut flow.

**Architecture:** New `Booking` slice mirrors the existing `Hospitality` slice's layering (Domain Entities/Ports → Application UseCases → Infrastructure Persistence/Messaging → Api Controllers). `RoomCategory` groups `Room`s for pricing/availability; `RatePlan` is a category's priced stay-length option; `BookingOrder` is the client's order, created `Pending` synchronously with a DB-enforced unique `IdempotencyKey`, then processed asynchronously by a MassTransit/RabbitMQ consumer partitioned by `RoomCategoryId` so two orders for the same category are never assigned a room concurrently. A `IWebhookSender` port fires after the consumer confirms/rejects.

**Tech Stack:** .NET 8, EF Core (Sqlite), MassTransit + RabbitMQ.Client, xUnit + Moq + FluentAssertions, Docker Compose (RabbitMQ).

## Global Constraints

- Domain and Application layers must never reference Infrastructure directly — `Program.cs` is the only composition root (see `Program.cs:1-5` comment block).
- Use cases: one interface `IXUseCase` per use case with a single `Execute(...)` method, implemented by a class of the matching name minus `I` — follow `ICheckInUseCase`/`CheckInUseCase` pattern.
- Commands are C# `record`s in a `*Commands.cs` file per slice (see `Application/UseCases/Hospitality/HospitalityCommands.cs`).
- DTOs (API request/response shapes) live in `Application/DTOs/*.cs` as records, separate from Commands; controllers map DTO → Command.
- Tests live under `CheckInApp.Tests/<mirrored path>`, use xUnit `[Fact]`, `Moq` for repository/port mocks, `FluentAssertions` for asserts. Constructor builds the SUT with mocks (see `CheckInUseCaseTests.cs:16-21`).
- New entities are plain mutable classes with public getters/setters (see `Room.cs`, `Reservation.cs`) — this project does not use the DDD-style private-setter pattern except for `User`. Follow the `Room`/`Reservation` style for `RoomCategory`, `RatePlan`, `BookingOrder`.
- All new repositories go through `AppDbContext` exactly like `RoomRepository`/`ReservationRepository` (constructor-injected `AppDbContext`, synchronous EF calls — the whole codebase uses sync EF Core calls, not async, so stay consistent).
- Register every new repository/use case/service in `Program.cs` under the matching existing comment block ("Output Adapters", "Use Cases (Application Layer)").

---

## File Structure

```
Domain/
  Entities/
    RoomCategory.cs        (new)
    RatePlan.cs             (new)
    BookingOrder.cs          (new)
  Enums/
    BookingStatus.cs         (new)
  Ports/
    IRoomCategoryRepository.cs   (new)
    IRatePlanRepository.cs       (new)
    IBookingOrderRepository.cs   (new)
    IWebhookSender.cs             (new)
    IBookingMessagePublisher.cs   (new)
Application/
  UseCases/Booking/
    BookingCommands.cs                          (new — CreateBookingCommand)
    IListAvailableRoomsUseCase.cs / ListAvailableRoomsUseCase.cs   (new)
    ICreateBookingUseCase.cs / CreateBookingUseCase.cs             (new)
    IGetBookingUseCase.cs / GetBookingUseCase.cs                   (new)
    ProcessBookingMessage.cs                     (new — MassTransit message contract)
    IProcessBookingUseCase.cs / ProcessBookingUseCase.cs           (new — logic invoked by the consumer)
  DTOs/
    BookingDtos.cs           (new)
Infrastructure/
  Persistence/
    AppDbContext.cs                        (modify — add 3 DbSets + unique index + seed RoomCategory/RatePlan/Room.RoomCategoryId)
    Repositories/
      RoomCategoryRepository.cs   (new)
      RatePlanRepository.cs        (new)
      BookingOrderRepository.cs    (new)
  Messaging/
    ProcessBookingConsumer.cs        (new)
    MassTransitBookingMessagePublisher.cs   (new)
  Webhooks/
    HttpWebhookSender.cs         (new)
Domain/Entities/Room.cs             (modify — add RoomCategoryId)
Api/
  Controllers/
    BookingController.cs        (new)
  Program.cs                    (modify — MassTransit config, new DI registrations)
appsettings.json                 (modify — add RabbitMq + Webhooks sections)
docker-compose.yml                (new — RabbitMQ)
CheckInApp.csproj                (modify — add MassTransit, MassTransit.RabbitMQ packages)
Migrations/                       (new migration, EF-generated)
CheckInApp.Tests/Application/UseCases/Booking/
  CreateBookingUseCaseTests.cs        (new)
  ListAvailableRoomsUseCaseTests.cs   (new)
  ProcessBookingUseCaseTests.cs       (new)
```

**Interfaces summary (cross-task contract):**

- `RoomCategory { int Id; string Name; int MaxCapacity; int MinStayDays; int MaxStayDays; }`
- `RatePlan { int Id; int RoomCategoryId; string Name; int HoursPackage; decimal PricePerDay; }`
- `BookingOrder { int Id; string IdempotencyKey; string Cpf; string GuestName; int GuestCount; int RoomCategoryId; int RatePlanId; int? RoomId; DateTime CheckInDate; DateTime CheckOutDate; BookingStatus Status; decimal TotalPrice; DateTime CreatedAt; }`
- `enum BookingStatus { Pending, Confirmed, Rejected }`
- `Room` gains `int RoomCategoryId { get; set; }`
- `IRoomCategoryRepository { RoomCategory[] ListCategoriesWithAvailability(DateTime checkIn, DateTime checkOut, int guestCount); RoomCategory? GetById(int id); }`
- `IRatePlanRepository { RatePlan[] ListByCategory(int roomCategoryId); RatePlan? GetById(int id); }`
- `IBookingOrderRepository { BookingOrder? GetByIdempotencyKey(string key); BookingOrder AddBookingOrder(BookingOrder order); BookingOrder? GetById(int id); void UpdateBookingOrder(BookingOrder order); Room? TryAssignRoom(BookingOrder order); }`
- `IWebhookSender { void SendBookingResult(BookingOrder order); }`
- `IBookingMessagePublisher { void PublishProcessBooking(int bookingOrderId, int roomCategoryId); }`
- `ProcessBookingMessage(int BookingOrderId, int RoomCategoryId)` — MassTransit message record
- `ICreateBookingUseCase { BookingOrder Execute(CreateBookingCommand command); }`
- `CreateBookingCommand(string IdempotencyKey, string Cpf, string GuestName, int GuestCount, int RoomCategoryId, int RatePlanId, DateTime CheckInDate, DateTime CheckOutDate)`
- `IListAvailableRoomsUseCase { AvailableRoomCategoryDto[] Execute(ListAvailableRoomsQuery query); }`
- `ListAvailableRoomsQuery(DateTime CheckIn, DateTime CheckOut, int GuestCount)`
- `IGetBookingUseCase { BookingOrder Execute(int bookingOrderId); }`
- `IProcessBookingUseCase { void Execute(int bookingOrderId); }` — called by `ProcessBookingConsumer.Consume(...)`

---

### Task 1: `BookingStatus` enum + `RoomCategory`, `RatePlan`, `BookingOrder` entities + `Room.RoomCategoryId`

**Files:**
- Create: `Domain/Enums/BookingStatus.cs`
- Create: `Domain/Entities/RoomCategory.cs`
- Create: `Domain/Entities/RatePlan.cs`
- Create: `Domain/Entities/BookingOrder.cs`
- Modify: `Domain/Entities/Room.cs`
- Test: `CheckInApp.Tests/Domain/Entities/BookingOrderTests.cs`

**Interfaces:**
- Produces: exact entity shapes listed in "Interfaces summary" above. All later tasks depend on these exact property names/types.

- [ ] **Step 1: Write the failing test for default BookingOrder state**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CheckInApp.Tests.Domain.Entities;

public class BookingOrderTests
{
    [Fact]
    public void NewBookingOrder_ShouldDefaultToPendingStatus()
    {
        var order = new BookingOrder();

        order.Status.Should().Be(BookingStatus.Pending);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~BookingOrderTests"`
Expected: FAIL — compile error, `CheckInApp.Domain.Entities.BookingOrder` does not exist.

- [ ] **Step 3: Create `BookingStatus` enum**

```csharp
namespace CheckInApp.Domain.Enums;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected
}
```

- [ ] **Step 4: Create `RoomCategory` entity**

```csharp
namespace CheckInApp.Domain.Entities;

public class RoomCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int MinStayDays { get; set; }
    public int MaxStayDays { get; set; }
}
```

- [ ] **Step 5: Create `RatePlan` entity**

```csharp
namespace CheckInApp.Domain.Entities;

public class RatePlan
{
    public int Id { get; set; }
    public int RoomCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HoursPackage { get; set; }
    public decimal PricePerDay { get; set; }
}
```

- [ ] **Step 6: Create `BookingOrder` entity**

```csharp
using CheckInApp.Domain.Enums;

namespace CheckInApp.Domain.Entities;

public class BookingOrder
{
    public int Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int RoomCategoryId { get; set; }
    public int RatePlanId { get; set; }
    public int? RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 7: Add `RoomCategoryId` to `Room`**

In `Domain/Entities/Room.cs`, add a property after `MaxCapacity`:

```csharp
public int MaxCapacity { get; set; }
public int RoomCategoryId { get; set; }
public Guest? CurrentGuest { get; set; }
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~BookingOrderTests"`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add Domain/Enums/BookingStatus.cs Domain/Entities/RoomCategory.cs Domain/Entities/RatePlan.cs Domain/Entities/BookingOrder.cs Domain/Entities/Room.cs CheckInApp.Tests/Domain/Entities/BookingOrderTests.cs
git commit -m "feat: add booking domain entities (RoomCategory, RatePlan, BookingOrder)"
```

---

### Task 2: Domain Ports for Booking

**Files:**
- Create: `Domain/Ports/IRoomCategoryRepository.cs`
- Create: `Domain/Ports/IRatePlanRepository.cs`
- Create: `Domain/Ports/IBookingOrderRepository.cs`
- Create: `Domain/Ports/IWebhookSender.cs`
- Create: `Domain/Ports/IBookingMessagePublisher.cs`

**Interfaces:**
- Consumes: `RoomCategory`, `RatePlan`, `BookingOrder`, `Room` from Task 1.
- Produces: exact port signatures below, depended on by all Application-layer use cases (Tasks 4-7) and all Infrastructure implementations (Task 3, Task 8, Task 9).

This task is interface-only (no test — there's no behavior to test on a port declaration; it mirrors `IRoomRepository`/`IReservationRepository` which also have no dedicated tests).

- [ ] **Step 1: Create `IRoomCategoryRepository`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IRoomCategoryRepository
{
    RoomCategory[] ListCategoriesWithAvailability(DateTime checkIn, DateTime checkOut, int guestCount);
    RoomCategory? GetById(int id);
}
```

- [ ] **Step 2: Create `IRatePlanRepository`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IRatePlanRepository
{
    RatePlan[] ListByCategory(int roomCategoryId);
    RatePlan? GetById(int id);
}
```

- [ ] **Step 3: Create `IBookingOrderRepository`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IBookingOrderRepository
{
    BookingOrder? GetByIdempotencyKey(string idempotencyKey);
    BookingOrder AddBookingOrder(BookingOrder order);
    BookingOrder? GetById(int id);
    void UpdateBookingOrder(BookingOrder order);
    Room? TryAssignRoom(BookingOrder order);
}
```

- [ ] **Step 4: Create `IWebhookSender`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IWebhookSender
{
    void SendBookingResult(BookingOrder order);
}
```

- [ ] **Step 5: Create `IBookingMessagePublisher`**

```csharp
namespace CheckInApp.Domain.Ports;

public interface IBookingMessagePublisher
{
    void PublishProcessBooking(int bookingOrderId, int roomCategoryId);
}
```

- [ ] **Step 6: Build the project to confirm it compiles**

Run: `dotnet build`
Expected: Build succeeded (0 errors) — ports have no implementations yet, but nothing references them yet either.

- [ ] **Step 7: Commit**

```bash
git add Domain/Ports/IRoomCategoryRepository.cs Domain/Ports/IRatePlanRepository.cs Domain/Ports/IBookingOrderRepository.cs Domain/Ports/IWebhookSender.cs Domain/Ports/IBookingMessagePublisher.cs
git commit -m "feat: add booking domain ports"
```

---

### Task 3: EF Core persistence — DbSets, unique index, migration, repositories

**Files:**
- Modify: `Infrastructure/Persistence/AppDbContext.cs`
- Create: `Infrastructure/Persistence/Repositories/RoomCategoryRepository.cs`
- Create: `Infrastructure/Persistence/Repositories/RatePlanRepository.cs`
- Create: `Infrastructure/Persistence/Repositories/BookingOrderRepository.cs`
- Create: EF migration (generated via `dotnet ef migrations add`)

**Interfaces:**
- Consumes: `IRoomCategoryRepository`, `IRatePlanRepository`, `IBookingOrderRepository` (Task 2); `RoomCategory`, `RatePlan`, `BookingOrder`, `Room`, `BookingStatus` (Task 1).
- Produces: working EF-backed implementations used by DI registration in Task 10.

- [ ] **Step 1: Add DbSets and model configuration to `AppDbContext`**

In `Infrastructure/Persistence/AppDbContext.cs`, add after the existing `DbSet<User> Users`:

```csharp
public DbSet<Room> Rooms { get; set; }
public DbSet<Reservation> Reservations { get; set; }
public DbSet<User> Users { get; set; }
public DbSet<RoomCategory> RoomCategories { get; set; }
public DbSet<RatePlan> RatePlans { get; set; }
public DbSet<BookingOrder> BookingOrders { get; set; }
```

Inside `OnModelCreating`, after the `User` configuration block and before the `Room.HasData` seed, add:

```csharp
modelBuilder.Entity<BookingOrder>()
    .HasIndex(b => b.IdempotencyKey)
    .IsUnique();
```

Then replace the existing `Room` seed block:

```csharp
modelBuilder.Entity<Room>().HasData(
    new Room { Id = 1, Number = 101, Status = RoomStatus.Available, DailyRate = 100.00m, MaxCapacity = 2 },
    new Room { Id = 2, Number = 102, Status = RoomStatus.Available, DailyRate = 150.00m, MaxCapacity = 3 },
    new Room { Id = 3, Number = 103, Status = RoomStatus.Available, DailyRate = 200.00m, MaxCapacity = 4 }
);
```

with (adds `RoomCategoryId` to each room, seeds one category + two rate plans):

```csharp
modelBuilder.Entity<RoomCategory>().HasData(
    new RoomCategory { Id = 1, Name = "Standard", MaxCapacity = 4, MinStayDays = 1, MaxStayDays = 14 }
);

modelBuilder.Entity<RatePlan>().HasData(
    new RatePlan { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = 150.00m },
    new RatePlan { Id = 2, RoomCategoryId = 1, Name = "8h", HoursPackage = 8, PricePerDay = 100.00m }
);

modelBuilder.Entity<Room>().HasData(
    new Room { Id = 1, Number = 101, Status = RoomStatus.Available, DailyRate = 100.00m, MaxCapacity = 2, RoomCategoryId = 1 },
    new Room { Id = 2, Number = 102, Status = RoomStatus.Available, DailyRate = 150.00m, MaxCapacity = 3, RoomCategoryId = 1 },
    new Room { Id = 3, Number = 103, Status = RoomStatus.Available, DailyRate = 200.00m, MaxCapacity = 4, RoomCategoryId = 1 }
);
```

Add the required using at the top of the file:

```csharp
using CheckInApp.Domain.Entities;
```
(already present — no change needed if it's already imported; verify before editing.)

- [ ] **Step 2: Build to confirm entity/model changes compile**

Run: `dotnet build`
Expected: Build succeeded (0 errors).

- [ ] **Step 3: Generate the EF Core migration**

Run: `dotnet ef migrations add AddBookingEntities`
Expected: New files created under `Migrations/`, e.g. `<timestamp>_AddBookingEntities.cs`. Command exits 0.

- [ ] **Step 4: Apply the migration to the local dev database to confirm it runs**

Run: `dotnet ef database update`
Expected: "Done." — no errors, `hotel.db` now has `RoomCategories`, `RatePlans`, `BookingOrders` tables and the unique index.

- [ ] **Step 5: Create `RoomCategoryRepository`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class RoomCategoryRepository : IRoomCategoryRepository
{
    private readonly AppDbContext _context;

    public RoomCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public RoomCategory[] ListCategoriesWithAvailability(DateTime checkIn, DateTime checkOut, int guestCount)
    {
        return _context.RoomCategories
            .Where(c => c.MaxCapacity >= guestCount)
            .Where(c => _context.Rooms.Any(r =>
                r.RoomCategoryId == c.Id &&
                !_context.BookingOrders.Any(b =>
                    b.RoomId == r.Id &&
                    b.Status == Domain.Enums.BookingStatus.Confirmed &&
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn)))
            .ToArray();
    }

    public RoomCategory? GetById(int id)
    {
        return _context.RoomCategories.FirstOrDefault(c => c.Id == id);
    }
}
```

- [ ] **Step 6: Create `RatePlanRepository`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class RatePlanRepository : IRatePlanRepository
{
    private readonly AppDbContext _context;

    public RatePlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public RatePlan[] ListByCategory(int roomCategoryId)
    {
        return _context.RatePlans.Where(p => p.RoomCategoryId == roomCategoryId).ToArray();
    }

    public RatePlan? GetById(int id)
    {
        return _context.RatePlans.FirstOrDefault(p => p.Id == id);
    }
}
```

- [ ] **Step 7: Create `BookingOrderRepository`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Infrastructure.Persistence.Repositories;

public class BookingOrderRepository : IBookingOrderRepository
{
    private readonly AppDbContext _context;

    public BookingOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public BookingOrder? GetByIdempotencyKey(string idempotencyKey)
    {
        return _context.BookingOrders.FirstOrDefault(b => b.IdempotencyKey == idempotencyKey);
    }

    public BookingOrder AddBookingOrder(BookingOrder order)
    {
        _context.BookingOrders.Add(order);
        _context.SaveChanges();
        return order;
    }

    public BookingOrder? GetById(int id)
    {
        return _context.BookingOrders.FirstOrDefault(b => b.Id == id);
    }

    public void UpdateBookingOrder(BookingOrder order)
    {
        _context.BookingOrders.Update(order);
        _context.SaveChanges();
    }

    public Room? TryAssignRoom(BookingOrder order)
    {
        using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

        var freeRoom = _context.Rooms
            .Where(r => r.RoomCategoryId == order.RoomCategoryId)
            .FirstOrDefault(r => !_context.BookingOrders.Any(b =>
                b.RoomId == r.Id &&
                b.Status == BookingStatus.Confirmed &&
                b.CheckInDate < order.CheckOutDate &&
                b.CheckOutDate > order.CheckInDate));

        if (freeRoom != null)
        {
            order.RoomId = freeRoom.Id;
            order.Status = BookingStatus.Confirmed;
        }
        else
        {
            order.Status = BookingStatus.Rejected;
        }

        _context.BookingOrders.Update(order);
        _context.SaveChanges();

        transaction.Commit();
        return freeRoom;
    }
}
```

- [ ] **Step 8: Build to confirm repositories compile**

Run: `dotnet build`
Expected: Build succeeded (0 errors).

- [ ] **Step 9: Commit**

```bash
git add Infrastructure/Persistence/AppDbContext.cs Infrastructure/Persistence/Repositories/RoomCategoryRepository.cs Infrastructure/Persistence/Repositories/RatePlanRepository.cs Infrastructure/Persistence/Repositories/BookingOrderRepository.cs Migrations/
git commit -m "feat: add EF Core persistence for booking entities + migration"
```

---

### Task 4: `CreateBookingUseCase` (idempotent order creation)

**Files:**
- Create: `Application/UseCases/Booking/BookingCommands.cs`
- Create: `Application/UseCases/Booking/ICreateBookingUseCase.cs`
- Create: `Application/UseCases/Booking/CreateBookingUseCase.cs`
- Test: `CheckInApp.Tests/Application/UseCases/Booking/CreateBookingUseCaseTests.cs`

**Interfaces:**
- Consumes: `IBookingOrderRepository`, `IRoomCategoryRepository`, `IRatePlanRepository`, `IBookingMessagePublisher` (Task 2/3); `RoomCategory`, `RatePlan`, `BookingOrder`, `BookingStatus` (Task 1).
- Produces: `ICreateBookingUseCase.Execute(CreateBookingCommand)` used by `BookingController` (Task 9).

- [ ] **Step 1: Write failing tests**

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class CreateBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly Mock<IRoomCategoryRepository> _roomCategoryRepositoryMock;
    private readonly Mock<IRatePlanRepository> _ratePlanRepositoryMock;
    private readonly Mock<IBookingMessagePublisher> _publisherMock;
    private readonly CreateBookingUseCase _sut;

    public CreateBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _roomCategoryRepositoryMock = new Mock<IRoomCategoryRepository>();
        _ratePlanRepositoryMock = new Mock<IRatePlanRepository>();
        _publisherMock = new Mock<IBookingMessagePublisher>();
        _sut = new CreateBookingUseCase(
            _bookingOrderRepositoryMock.Object,
            _roomCategoryRepositoryMock.Object,
            _ratePlanRepositoryMock.Object,
            _publisherMock.Object);
    }

    private static RoomCategory CreateCategory(int maxCapacity = 4, int minStayDays = 1, int maxStayDays = 14) =>
        new() { Id = 1, Name = "Standard", MaxCapacity = maxCapacity, MinStayDays = minStayDays, MaxStayDays = maxStayDays };

    private static RatePlan CreateRatePlan(decimal pricePerDay = 150m) =>
        new() { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = pricePerDay };

    private static CreateBookingCommand CreateValidCommand(string idempotencyKey = "key-1", int guestCount = 2, int stayDays = 3) =>
        new(
            IdempotencyKey: idempotencyKey,
            Cpf: "529.982.247-25",
            GuestName: "João Silva",
            GuestCount: guestCount,
            RoomCategoryId: 1,
            RatePlanId: 1,
            CheckInDate: new DateTime(2026, 8, 1),
            CheckOutDate: new DateTime(2026, 8, 1).AddDays(stayDays)
        );

    [Fact]
    public void Execute_ShouldCreatePendingOrder_AndPublishMessage_WhenNewRequest()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory());
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey("key-1")).Returns((BookingOrder?)null);
        _bookingOrderRepositoryMock
            .Setup(r => r.AddBookingOrder(It.IsAny<BookingOrder>()))
            .Returns<BookingOrder>(o => { o.Id = 42; return o; });

        var result = _sut.Execute(CreateValidCommand());

        result.Status.Should().Be(BookingStatus.Pending);
        result.Id.Should().Be(42);
        _publisherMock.Verify(p => p.PublishProcessBooking(42, 1), Times.Once);
    }

    [Fact]
    public void Execute_ShouldReturnExistingOrder_AndNotPublishAgain_WhenIdempotencyKeyAlreadyExists()
    {
        var existing = new BookingOrder { Id = 7, IdempotencyKey = "key-1", Status = BookingStatus.Confirmed };
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey("key-1")).Returns(existing);

        var result = _sut.Execute(CreateValidCommand());

        result.Should().BeSameAs(existing);
        _bookingOrderRepositoryMock.Verify(r => r.AddBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _publisherMock.Verify(p => p.PublishProcessBooking(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Execute_ShouldThrowException_WhenGuestCountExceedsCategoryCapacity()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory(maxCapacity: 2));
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);

        var command = CreateValidCommand(guestCount: 3);

        Action act = () => _sut.Execute(command);

        act.Should().Throw<Exception>().WithMessage("*capacity*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void Execute_ShouldThrowException_WhenStayDaysOutsideCategoryRange(int stayDays)
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory(minStayDays: 1, maxStayDays: 14));
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan());
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);

        var command = CreateValidCommand(stayDays: stayDays);

        Action act = () => _sut.Execute(command);

        act.Should().Throw<Exception>().WithMessage("*stay*");
    }

    [Fact]
    public void Execute_ShouldCalculateTotalPrice_AsPricePerDayTimesStayDays()
    {
        _roomCategoryRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateCategory());
        _ratePlanRepositoryMock.Setup(r => r.GetById(1)).Returns(CreateRatePlan(pricePerDay: 150m));
        _bookingOrderRepositoryMock.Setup(r => r.GetByIdempotencyKey(It.IsAny<string>())).Returns((BookingOrder?)null);
        _bookingOrderRepositoryMock
            .Setup(r => r.AddBookingOrder(It.IsAny<BookingOrder>()))
            .Returns<BookingOrder>(o => o);

        var result = _sut.Execute(CreateValidCommand(stayDays: 3));

        result.TotalPrice.Should().Be(450m);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~CreateBookingUseCaseTests"`
Expected: FAIL — compile errors, `CreateBookingUseCase`/`CreateBookingCommand`/`ICreateBookingUseCase` do not exist.

- [ ] **Step 3: Create `BookingCommands.cs`**

```csharp
namespace CheckInApp.Application.UseCases.Booking;

public record CreateBookingCommand(
    string IdempotencyKey,
    string Cpf,
    string GuestName,
    int GuestCount,
    int RoomCategoryId,
    int RatePlanId,
    DateTime CheckInDate,
    DateTime CheckOutDate
);
```

- [ ] **Step 4: Create `ICreateBookingUseCase.cs`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Booking;

public interface ICreateBookingUseCase
{
    BookingOrder Execute(CreateBookingCommand command);
}
```

- [ ] **Step 5: Create `CreateBookingUseCase.cs`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class CreateBookingUseCase : ICreateBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;
    private readonly IRoomCategoryRepository _roomCategoryRepository;
    private readonly IRatePlanRepository _ratePlanRepository;
    private readonly IBookingMessagePublisher _publisher;

    public CreateBookingUseCase(
        IBookingOrderRepository bookingOrderRepository,
        IRoomCategoryRepository roomCategoryRepository,
        IRatePlanRepository ratePlanRepository,
        IBookingMessagePublisher publisher)
    {
        _bookingOrderRepository = bookingOrderRepository;
        _roomCategoryRepository = roomCategoryRepository;
        _ratePlanRepository = ratePlanRepository;
        _publisher = publisher;
    }

    public BookingOrder Execute(CreateBookingCommand command)
    {
        var existing = _bookingOrderRepository.GetByIdempotencyKey(command.IdempotencyKey);
        if (existing != null)
            return existing;

        var category = _roomCategoryRepository.GetById(command.RoomCategoryId);
        if (category == null)
            throw new Exception("Room category not found");

        var ratePlan = _ratePlanRepository.GetById(command.RatePlanId);
        if (ratePlan == null)
            throw new Exception("Rate plan not found");

        if (command.GuestCount > category.MaxCapacity)
            throw new Exception("Guest count exceeds category capacity");

        var stayDays = (command.CheckOutDate.Date - command.CheckInDate.Date).Days;
        if (stayDays < category.MinStayDays || stayDays > category.MaxStayDays)
            throw new Exception("Stay length outside category's allowed range");

        var order = new BookingOrder
        {
            IdempotencyKey = command.IdempotencyKey,
            Cpf = command.Cpf,
            GuestName = command.GuestName,
            GuestCount = command.GuestCount,
            RoomCategoryId = command.RoomCategoryId,
            RatePlanId = command.RatePlanId,
            CheckInDate = command.CheckInDate,
            CheckOutDate = command.CheckOutDate,
            Status = BookingStatus.Pending,
            TotalPrice = ratePlan.PricePerDay * stayDays
        };

        order = _bookingOrderRepository.AddBookingOrder(order);
        _publisher.PublishProcessBooking(order.Id, order.RoomCategoryId);

        return order;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~CreateBookingUseCaseTests"`
Expected: PASS (5 tests)

- [ ] **Step 7: Commit**

```bash
git add Application/UseCases/Booking/BookingCommands.cs Application/UseCases/Booking/ICreateBookingUseCase.cs Application/UseCases/Booking/CreateBookingUseCase.cs CheckInApp.Tests/Application/UseCases/Booking/CreateBookingUseCaseTests.cs
git commit -m "feat: add CreateBookingUseCase with idempotency and validation"
```

---

### Task 5: `ListAvailableRoomsUseCase`

**Files:**
- Create: `Application/DTOs/BookingDtos.cs`
- Create: `Application/UseCases/Booking/IListAvailableRoomsUseCase.cs`
- Create: `Application/UseCases/Booking/ListAvailableRoomsUseCase.cs`
- Test: `CheckInApp.Tests/Application/UseCases/Booking/ListAvailableRoomsUseCaseTests.cs`

**Interfaces:**
- Consumes: `IRoomCategoryRepository`, `IRatePlanRepository` (Task 2/3).
- Produces: `IListAvailableRoomsUseCase.Execute(ListAvailableRoomsQuery)` returning `AvailableRoomCategoryDto[]`, used by `BookingController` (Task 9).

- [ ] **Step 1: Write failing test**

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class ListAvailableRoomsUseCaseTests
{
    private readonly Mock<IRoomCategoryRepository> _roomCategoryRepositoryMock;
    private readonly Mock<IRatePlanRepository> _ratePlanRepositoryMock;
    private readonly ListAvailableRoomsUseCase _sut;

    public ListAvailableRoomsUseCaseTests()
    {
        _roomCategoryRepositoryMock = new Mock<IRoomCategoryRepository>();
        _ratePlanRepositoryMock = new Mock<IRatePlanRepository>();
        _sut = new ListAvailableRoomsUseCase(_roomCategoryRepositoryMock.Object, _ratePlanRepositoryMock.Object);
    }

    [Fact]
    public void Execute_ShouldReturnCategoriesWithTheirRatePlans()
    {
        var checkIn = new DateTime(2026, 8, 1);
        var checkOut = new DateTime(2026, 8, 3);
        var category = new RoomCategory { Id = 1, Name = "Standard", MaxCapacity = 4, MinStayDays = 1, MaxStayDays = 14 };
        _roomCategoryRepositoryMock
            .Setup(r => r.ListCategoriesWithAvailability(checkIn, checkOut, 2))
            .Returns(new[] { category });
        _ratePlanRepositoryMock
            .Setup(r => r.ListByCategory(1))
            .Returns(new[] { new RatePlan { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = 150m } });

        var result = _sut.Execute(new ListAvailableRoomsQuery(checkIn, checkOut, 2));

        result.Should().HaveCount(1);
        result[0].RoomCategoryId.Should().Be(1);
        result[0].Name.Should().Be("Standard");
        result[0].RatePlans.Should().ContainSingle(p => p.Name == "12h" && p.PricePerDay == 150m);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ListAvailableRoomsUseCaseTests"`
Expected: FAIL — compile error, types don't exist yet.

- [ ] **Step 3: Create `BookingDtos.cs`**

```csharp
namespace CheckInApp.Application.DTOs;

public record RatePlanDto(
    int RatePlanId,
    string Name,
    int HoursPackage,
    decimal PricePerDay
);

public record AvailableRoomCategoryDto(
    int RoomCategoryId,
    string Name,
    int MaxCapacity,
    int MinStayDays,
    int MaxStayDays,
    RatePlanDto[] RatePlans
);

public record CreateBookingRequestDto(
    string Cpf,
    string GuestName,
    int GuestCount,
    int RoomCategoryId,
    int RatePlanId,
    DateTime CheckInDate,
    DateTime CheckOutDate
);
```

- [ ] **Step 4: Create `IListAvailableRoomsUseCase.cs`**

```csharp
using CheckInApp.Application.DTOs;

namespace CheckInApp.Application.UseCases.Booking;

public record ListAvailableRoomsQuery(DateTime CheckIn, DateTime CheckOut, int GuestCount);

public interface IListAvailableRoomsUseCase
{
    AvailableRoomCategoryDto[] Execute(ListAvailableRoomsQuery query);
}
```

- [ ] **Step 5: Create `ListAvailableRoomsUseCase.cs`**

```csharp
using CheckInApp.Application.DTOs;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class ListAvailableRoomsUseCase : IListAvailableRoomsUseCase
{
    private readonly IRoomCategoryRepository _roomCategoryRepository;
    private readonly IRatePlanRepository _ratePlanRepository;

    public ListAvailableRoomsUseCase(IRoomCategoryRepository roomCategoryRepository, IRatePlanRepository ratePlanRepository)
    {
        _roomCategoryRepository = roomCategoryRepository;
        _ratePlanRepository = ratePlanRepository;
    }

    public AvailableRoomCategoryDto[] Execute(ListAvailableRoomsQuery query)
    {
        var categories = _roomCategoryRepository.ListCategoriesWithAvailability(query.CheckIn, query.CheckOut, query.GuestCount);

        return categories.Select(category =>
        {
            var ratePlans = _ratePlanRepository.ListByCategory(category.Id)
                .Select(p => new RatePlanDto(p.Id, p.Name, p.HoursPackage, p.PricePerDay))
                .ToArray();

            return new AvailableRoomCategoryDto(
                category.Id,
                category.Name,
                category.MaxCapacity,
                category.MinStayDays,
                category.MaxStayDays,
                ratePlans);
        }).ToArray();
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ListAvailableRoomsUseCaseTests"`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add Application/DTOs/BookingDtos.cs Application/UseCases/Booking/IListAvailableRoomsUseCase.cs Application/UseCases/Booking/ListAvailableRoomsUseCase.cs CheckInApp.Tests/Application/UseCases/Booking/ListAvailableRoomsUseCaseTests.cs
git commit -m "feat: add ListAvailableRoomsUseCase for room availability catalog"
```

---

### Task 6: `GetBookingUseCase`

**Files:**
- Create: `Application/UseCases/Booking/IGetBookingUseCase.cs`
- Create: `Application/UseCases/Booking/GetBookingUseCase.cs`
- Test: `CheckInApp.Tests/Application/UseCases/Booking/GetBookingUseCaseTests.cs`

**Interfaces:**
- Consumes: `IBookingOrderRepository` (Task 2/3).
- Produces: `IGetBookingUseCase.Execute(int)` used by `BookingController` (Task 9).

- [ ] **Step 1: Write failing test**

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class GetBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly GetBookingUseCase _sut;

    public GetBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _sut = new GetBookingUseCase(_bookingOrderRepositoryMock.Object);
    }

    [Fact]
    public void Execute_ShouldReturnOrder_WhenFound()
    {
        var order = new BookingOrder { Id = 5 };
        _bookingOrderRepositoryMock.Setup(r => r.GetById(5)).Returns(order);

        var result = _sut.Execute(5);

        result.Should().BeSameAs(order);
    }

    [Fact]
    public void Execute_ShouldThrowException_WhenNotFound()
    {
        _bookingOrderRepositoryMock.Setup(r => r.GetById(999)).Returns((BookingOrder?)null);

        Action act = () => _sut.Execute(999);

        act.Should().Throw<Exception>().WithMessage("*not found*");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~GetBookingUseCaseTests"`
Expected: FAIL — compile error, types don't exist yet.

- [ ] **Step 3: Create `IGetBookingUseCase.cs`**

```csharp
using CheckInApp.Domain.Entities;

namespace CheckInApp.Application.UseCases.Booking;

public interface IGetBookingUseCase
{
    BookingOrder Execute(int bookingOrderId);
}
```

- [ ] **Step 4: Create `GetBookingUseCase.cs`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class GetBookingUseCase : IGetBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;

    public GetBookingUseCase(IBookingOrderRepository bookingOrderRepository)
    {
        _bookingOrderRepository = bookingOrderRepository;
    }

    public BookingOrder Execute(int bookingOrderId)
    {
        var order = _bookingOrderRepository.GetById(bookingOrderId);
        if (order == null)
            throw new Exception("Booking order not found");

        return order;
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~GetBookingUseCaseTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add Application/UseCases/Booking/IGetBookingUseCase.cs Application/UseCases/Booking/GetBookingUseCase.cs CheckInApp.Tests/Application/UseCases/Booking/GetBookingUseCaseTests.cs
git commit -m "feat: add GetBookingUseCase for booking status lookup"
```

---

### Task 7: `ProcessBookingUseCase` (room assignment + confirm/reject + webhook trigger)

**Files:**
- Create: `Application/UseCases/Booking/ProcessBookingMessage.cs`
- Create: `Application/UseCases/Booking/IProcessBookingUseCase.cs`
- Create: `Application/UseCases/Booking/ProcessBookingUseCase.cs`
- Test: `CheckInApp.Tests/Application/UseCases/Booking/ProcessBookingUseCaseTests.cs`

**Interfaces:**
- Consumes: `IBookingOrderRepository`, `IWebhookSender` (Task 2/3); `BookingOrder`, `BookingStatus`, `Room` (Task 1).
- Produces: `IProcessBookingUseCase.Execute(int bookingOrderId)`, called from `ProcessBookingConsumer.Consume(...)` (Task 8). `ProcessBookingMessage(int BookingOrderId, int RoomCategoryId)` is the MassTransit message contract published by `MassTransitBookingMessagePublisher` (Task 8) and consumed by `ProcessBookingConsumer` (Task 8). `RoomCategoryId` is carried on the message solely so the receive endpoint can partition by category (Task 10); the use case still re-fetches the order from the DB and does not need the field itself.

- [ ] **Step 1: Write failing tests**

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;
using FluentAssertions;
using Moq;
using Xunit;

namespace CheckInApp.Tests.Application.UseCases.Booking;

public class ProcessBookingUseCaseTests
{
    private readonly Mock<IBookingOrderRepository> _bookingOrderRepositoryMock;
    private readonly Mock<IWebhookSender> _webhookSenderMock;
    private readonly ProcessBookingUseCase _sut;

    public ProcessBookingUseCaseTests()
    {
        _bookingOrderRepositoryMock = new Mock<IBookingOrderRepository>();
        _webhookSenderMock = new Mock<IWebhookSender>();
        _sut = new ProcessBookingUseCase(_bookingOrderRepositoryMock.Object, _webhookSenderMock.Object);
    }

    private static BookingOrder CreatePendingOrder() => new()
    {
        Id = 10,
        RoomCategoryId = 1,
        CheckInDate = new DateTime(2026, 8, 1),
        CheckOutDate = new DateTime(2026, 8, 3),
        Status = BookingStatus.Pending
    };

    [Fact]
    public void Execute_ShouldConfirmOrder_AndAssignRoom_WhenFreeRoomExists()
    {
        var order = CreatePendingOrder();
        var freeRoom = new Room { Id = 3, Number = 103, RoomCategoryId = 1 };
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);
        _bookingOrderRepositoryMock
            .Setup(r => r.TryAssignRoom(order))
            .Returns<BookingOrder>(o =>
            {
                o.RoomId = freeRoom.Id;
                o.Status = BookingStatus.Confirmed;
                return freeRoom;
            });

        _sut.Execute(10);

        order.Status.Should().Be(BookingStatus.Confirmed);
        order.RoomId.Should().Be(3);
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(order), Times.Once);
    }

    [Fact]
    public void Execute_ShouldRejectOrder_WhenNoFreeRoomExists()
    {
        var order = CreatePendingOrder();
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);
        _bookingOrderRepositoryMock
            .Setup(r => r.TryAssignRoom(order))
            .Returns<BookingOrder>(o =>
            {
                o.Status = BookingStatus.Rejected;
                return null;
            });

        _sut.Execute(10);

        order.Status.Should().Be(BookingStatus.Rejected);
        order.RoomId.Should().BeNull();
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(order), Times.Once);
    }

    [Fact]
    public void Execute_ShouldDoNothing_WhenOrderAlreadyProcessed()
    {
        var order = CreatePendingOrder();
        order.Status = BookingStatus.Confirmed;
        _bookingOrderRepositoryMock.Setup(r => r.GetById(10)).Returns(order);

        _sut.Execute(10);

        _bookingOrderRepositoryMock.Verify(r => r.TryAssignRoom(It.IsAny<BookingOrder>()), Times.Never);
        _bookingOrderRepositoryMock.Verify(r => r.UpdateBookingOrder(It.IsAny<BookingOrder>()), Times.Never);
        _webhookSenderMock.Verify(w => w.SendBookingResult(It.IsAny<BookingOrder>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~ProcessBookingUseCaseTests"`
Expected: FAIL — compile error, types don't exist yet.

- [ ] **Step 3: Create `ProcessBookingMessage.cs`**

```csharp
namespace CheckInApp.Application.UseCases.Booking;

public record ProcessBookingMessage(int BookingOrderId, int RoomCategoryId);
```

- [ ] **Step 4: Create `IProcessBookingUseCase.cs`**

```csharp
namespace CheckInApp.Application.UseCases.Booking;

public interface IProcessBookingUseCase
{
    void Execute(int bookingOrderId);
}
```

- [ ] **Step 5: Create `ProcessBookingUseCase.cs`**

```csharp
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.Ports;

namespace CheckInApp.Application.UseCases.Booking;

public class ProcessBookingUseCase : IProcessBookingUseCase
{
    private readonly IBookingOrderRepository _bookingOrderRepository;
    private readonly IWebhookSender _webhookSender;

    public ProcessBookingUseCase(IBookingOrderRepository bookingOrderRepository, IWebhookSender webhookSender)
    {
        _bookingOrderRepository = bookingOrderRepository;
        _webhookSender = webhookSender;
    }

    public void Execute(int bookingOrderId)
    {
        var order = _bookingOrderRepository.GetById(bookingOrderId);
        if (order == null || order.Status != BookingStatus.Pending)
            return;

        // TryAssignRoom performs the free-room lookup, the RoomId/Status mutation, and the
        // SaveChanges persistence all inside one Serializable transaction, so no separate
        // UpdateBookingOrder call is needed (or safe to make) after this.
        _bookingOrderRepository.TryAssignRoom(order);

        _webhookSender.SendBookingResult(order);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~ProcessBookingUseCaseTests"`
Expected: PASS (3 tests)

- [ ] **Step 7: Commit**

```bash
git add Application/UseCases/Booking/ProcessBookingMessage.cs Application/UseCases/Booking/IProcessBookingUseCase.cs Application/UseCases/Booking/ProcessBookingUseCase.cs CheckInApp.Tests/Application/UseCases/Booking/ProcessBookingUseCaseTests.cs
git commit -m "feat: add ProcessBookingUseCase for room assignment and webhook trigger"
```

---

### Task 8: MassTransit/RabbitMQ wiring (publisher + consumer) + HTTP webhook sender

**Files:**
- Modify: `CheckInApp.csproj`
- Create: `Infrastructure/Messaging/MassTransitBookingMessagePublisher.cs`
- Create: `Infrastructure/Messaging/ProcessBookingConsumer.cs`
- Create: `Infrastructure/Webhooks/HttpWebhookSender.cs`
- Create: `docker-compose.yml`
- Modify: `appsettings.json`
- Modify: `appsettings.Development.json`

**Interfaces:**
- Consumes: `IBookingMessagePublisher`, `IWebhookSender` (Task 2); `ProcessBookingMessage` (Task 7); `IProcessBookingUseCase` (Task 7).
- Produces: concrete adapters registered in Task 10 (`Program.cs`).

- [ ] **Step 1: Add MassTransit packages to `CheckInApp.csproj`**

In `CheckInApp.csproj`, inside the existing `<ItemGroup>` with `PackageReference`s, add:

```xml
<PackageReference Include="MassTransit" Version="8.2.5" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.2.5" />
```

- [ ] **Step 2: Restore packages**

Run: `dotnet restore`
Expected: Restore completed, 0 errors.

- [ ] **Step 3: Create `docker-compose.yml`**

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```

- [ ] **Step 4: Add RabbitMq and Webhooks config sections to `appsettings.json`**

In `appsettings.json`, add after the `"Jwt"` block:

```json
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Webhooks": {
    "BookingConfirmed": "https://webhook.site/replace-with-real-endpoint"
  },
```

(Keep valid JSON — this inserts before `"AllowedHosts"`.)

- [ ] **Step 5: Create `MassTransitBookingMessagePublisher.cs`**

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Domain.Ports;
using MassTransit;

namespace CheckInApp.Infrastructure.Messaging;

public class MassTransitBookingMessagePublisher : IBookingMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitBookingMessagePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public void PublishProcessBooking(int bookingOrderId, int roomCategoryId)
    {
        _publishEndpoint.Publish(new ProcessBookingMessage(bookingOrderId, roomCategoryId)).GetAwaiter().GetResult();
    }
}
```

- [ ] **Step 6: Create `ProcessBookingConsumer.cs`**

```csharp
using CheckInApp.Application.UseCases.Booking;
using MassTransit;

namespace CheckInApp.Infrastructure.Messaging;

public class ProcessBookingConsumer : IConsumer<ProcessBookingMessage>
{
    private readonly IProcessBookingUseCase _processBookingUseCase;

    public ProcessBookingConsumer(IProcessBookingUseCase processBookingUseCase)
    {
        _processBookingUseCase = processBookingUseCase;
    }

    public Task Consume(ConsumeContext<ProcessBookingMessage> context)
    {
        _processBookingUseCase.Execute(context.Message.BookingOrderId);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 7: Create `HttpWebhookSender.cs`**

```csharp
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CheckInApp.Infrastructure.Webhooks;

public class HttpWebhookSender : IWebhookSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpWebhookSender> _logger;

    public HttpWebhookSender(HttpClient httpClient, IConfiguration configuration, ILogger<HttpWebhookSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public void SendBookingResult(BookingOrder order)
    {
        var url = _configuration["Webhooks:BookingConfirmed"];
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            var payload = new
            {
                order.Id,
                order.Status,
                order.RoomId,
                order.TotalPrice,
                order.Cpf
            };

            _httpClient.PostAsJsonAsync(url, payload).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking webhook for order {OrderId}", order.Id);
        }
    }
}
```

Add the required using at the top for `PostAsJsonAsync`:

```csharp
using System.Net.Http.Json;
```

- [ ] **Step 8: Build to confirm the messaging/webhook layer compiles**

Run: `dotnet build`
Expected: Build succeeded (0 errors).

- [ ] **Step 9: Commit**

```bash
git add CheckInApp.csproj appsettings.json docker-compose.yml Infrastructure/Messaging/MassTransitBookingMessagePublisher.cs Infrastructure/Messaging/ProcessBookingConsumer.cs Infrastructure/Webhooks/HttpWebhookSender.cs
git commit -m "feat: add MassTransit/RabbitMQ wiring and HTTP webhook sender"
```

---

### Task 9: `BookingController` API endpoints

**Files:**
- Create: `Api/Controllers/BookingController.cs`

**Interfaces:**
- Consumes: `IListAvailableRoomsUseCase`, `ICreateBookingUseCase`, `IGetBookingUseCase` (Tasks 4-6); `AvailableRoomCategoryDto`, `CreateBookingRequestDto` (Task 5).

No new unit test here — controllers in this codebase are thin mapping layers with no dedicated tests (see `HospitalityController`, which has none). Verified instead by manual run in Task 11.

- [ ] **Step 1: Create `BookingController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Application.DTOs;

namespace CheckInApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IListAvailableRoomsUseCase _listAvailableRoomsUseCase;
    private readonly ICreateBookingUseCase _createBookingUseCase;
    private readonly IGetBookingUseCase _getBookingUseCase;

    public BookingController(
        IListAvailableRoomsUseCase listAvailableRoomsUseCase,
        ICreateBookingUseCase createBookingUseCase,
        IGetBookingUseCase getBookingUseCase)
    {
        _listAvailableRoomsUseCase = listAvailableRoomsUseCase;
        _createBookingUseCase = createBookingUseCase;
        _getBookingUseCase = getBookingUseCase;
    }

    [HttpGet("rooms/available")]
    public IActionResult ListAvailableRooms([FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int guestCount)
    {
        var query = new ListAvailableRoomsQuery(checkIn, checkOut, guestCount);
        var result = _listAvailableRoomsUseCase.Execute(query);
        return Ok(result);
    }

    [HttpPost("bookings")]
    public IActionResult CreateBooking([FromHeader(Name = "Idempotency-Key")] string idempotencyKey, [FromBody] CreateBookingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest("Idempotency-Key header is required");

        var command = new CreateBookingCommand(
            IdempotencyKey: idempotencyKey,
            Cpf: request.Cpf,
            GuestName: request.GuestName,
            GuestCount: request.GuestCount,
            RoomCategoryId: request.RoomCategoryId,
            RatePlanId: request.RatePlanId,
            CheckInDate: request.CheckInDate,
            CheckOutDate: request.CheckOutDate
        );

        var order = _createBookingUseCase.Execute(command);
        return AcceptedAtAction(nameof(GetBooking), new { id = order.Id }, order);
    }

    [HttpGet("bookings/{id}")]
    public IActionResult GetBooking(int id)
    {
        var order = _getBookingUseCase.Execute(id);
        return Ok(order);
    }
}
```

- [ ] **Step 2: Build to confirm the controller compiles**

Run: `dotnet build`
Expected: Build succeeded (0 errors).

- [ ] **Step 3: Commit**

```bash
git add Api/Controllers/BookingController.cs
git commit -m "feat: add BookingController with availability, create, and status endpoints"
```

---

### Task 10: Wire everything in `Program.cs`

**Files:**
- Modify: `Program.cs`

**Interfaces:**
- Consumes: every port and its Infrastructure implementation from Tasks 1-9.

- [ ] **Step 1: Add repository/service registrations**

In `Program.cs`, after the existing repository registrations block:

```csharp
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

add:

```csharp
builder.Services.AddScoped<IRoomCategoryRepository, RoomCategoryRepository>();
builder.Services.AddScoped<IRatePlanRepository, RatePlanRepository>();
builder.Services.AddScoped<IBookingOrderRepository, BookingOrderRepository>();
builder.Services.AddScoped<IBookingMessagePublisher, MassTransitBookingMessagePublisher>();
builder.Services.AddHttpClient<IWebhookSender, HttpWebhookSender>();
```

- [ ] **Step 2: Add use case registrations**

After the existing use case block:

```csharp
builder.Services.AddScoped<ICheckInUseCase, CheckInUseCase>();
builder.Services.AddScoped<ICheckOutUseCase, CheckOutUseCase>();
builder.Services.AddScoped<IListReservationsUseCase, ListReservationsUseCase>();
builder.Services.AddScoped<IListReservationByCpfUseCase, ListReservationByCpfUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<ISignupUseCase, SignupUseCase>();
```

add:

```csharp
builder.Services.AddScoped<IListAvailableRoomsUseCase, ListAvailableRoomsUseCase>();
builder.Services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();
builder.Services.AddScoped<IGetBookingUseCase, GetBookingUseCase>();
builder.Services.AddScoped<IProcessBookingUseCase, ProcessBookingUseCase>();
```

- [ ] **Step 3: Add MassTransit configuration**

After the use case registrations, before `// --- Input Adapters ---`:

```csharp
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

            var partitioner = e.CreatePartitioner(8);
            e.UsePartitioner<ProcessBookingMessage>(partitioner, m => new Guid(m.Message.RoomCategoryId, 0, 0, new byte[8]));
        });
    });
});
```

- [ ] **Step 4: Add required usings at the top of `Program.cs`**

Add alongside existing usings:

```csharp
using CheckInApp.Application.UseCases.Booking;
using CheckInApp.Infrastructure.Messaging;
using CheckInApp.Infrastructure.Webhooks;
```

- [ ] **Step 5: Build the whole solution**

Run: `dotnet build`
Expected: Build succeeded (0 errors).

- [ ] **Step 6: Run the full test suite**

Run: `dotnet test`
Expected: All tests pass (existing Hospitality/Identity/VO tests + new Booking tests).

- [ ] **Step 7: Commit**

```bash
git add Program.cs
git commit -m "feat: wire booking use cases, repositories, and RabbitMQ consumer into DI"
```

---

### Task 11: Manual end-to-end verification

**Files:** none (verification only)

**Interfaces:** none — this task exercises the full stack built in Tasks 1-10.

- [ ] **Step 1: Start RabbitMQ**

Run: `docker compose up -d rabbitmq`
Expected: Container starts; `docker compose ps` shows `rabbitmq` as `running`/`healthy`.

- [ ] **Step 2: Start the API**

Run: `dotnet run`
Expected: App starts, listens on the configured port, no startup exceptions (MassTransit connects to RabbitMQ successfully — check console log for "Bus started").

- [ ] **Step 3: Check available rooms**

Run: `curl -s "http://localhost:5000/api/booking/rooms/available?checkIn=2026-08-01&checkOut=2026-08-03&guestCount=2"`
Expected: JSON array with the seeded "Standard" category and its two rate plans (12h/8h).

(Adjust the port to whatever `dotnet run` prints, e.g. `https://localhost:7xxx` — use `-k` with curl if HTTPS with a dev cert.)

- [ ] **Step 4: Create a booking**

Run:
```bash
curl -s -X POST "http://localhost:5000/api/booking/bookings" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-1" \
  -d '{"cpf":"529.982.247-25","guestName":"João Silva","guestCount":2,"roomCategoryId":1,"ratePlanId":1,"checkInDate":"2026-08-01","checkOutDate":"2026-08-03"}'
```
Expected: 202 Accepted with the order body, `"status":"Pending"` (or `0` if enum serializes numerically).

- [ ] **Step 5: Confirm idempotency — repeat the exact same request**

Run the same `curl` command from Step 4 again with the same `Idempotency-Key: test-key-1`.
Expected: Same order `Id` returned, not a new one — confirms no duplicate was created.

- [ ] **Step 6: Poll booking status until the consumer processes it**

Run: `curl -s "http://localhost:5000/api/booking/bookings/<id-from-step-4>"`
Expected: After a short delay, `"status":"Confirmed"` and a `roomId` populated (one of the seeded rooms 1/2/3).

- [ ] **Step 7: Verify double-booking rejection**

Repeat Step 4 with 3 different `Idempotency-Key` values (e.g. `test-key-2`, `test-key-3`, `test-key-4`) for the same category/dates as Step 4, back-to-back. There are only 3 seeded rooms in category 1.
Expected: Polling all 4 orders' statuses eventually shows exactly 3 `Confirmed` (one per distinct `roomId`, no two sharing a `roomId`) and 1 `Rejected` — proving the partitioned consumer never double-assigns a room.

- [ ] **Step 8: Verify webhook fired**

Check the console log output from `dotnet run` for the `HttpWebhookSender` — either a successful POST or (if using a placeholder URL) a logged `LogWarning` for the failed POST. Either confirms `SendBookingResult` was invoked after processing.

- [ ] **Step 9: Stop RabbitMQ**

Run: `docker compose down`
Expected: Container stopped and removed.

---

## Self-Review Notes

- **Spec coverage:** availability listing (Task 5/9), rate-plan/category model (Task 1/3), min/max stay + capacity validation (Task 4), same-room-never-double-booked consistency (Task 3 `TryAssignRoom` + Task 8 partitioned consumer), queue (Task 8/10 MassTransit+RabbitMQ), webhook (Task 8 `HttpWebhookSender`), idempotency (Task 1 unique index + Task 4 `GetByIdempotencyKey` check) — all covered.
- **Placeholder scan:** no TBD/TODO; the webhook URL in `appsettings.json` is a literal placeholder value the user must replace with a real endpoint before Task 11 Step 8 will show a successful POST — this is called out explicitly, not left implicit.
- **Type consistency:** `BookingOrder`, `RoomCategory`, `RatePlan`, `BookingStatus` property names are identical across Tasks 1, 3, 4, 5, 6, 7. `ProcessBookingMessage(int BookingOrderId, int RoomCategoryId)` matches between Task 7 (definition) and Task 8 (consumer/publisher usage); `RoomCategoryId` is also the partition key used in Task 10's `ReceiveEndpoint` config.
