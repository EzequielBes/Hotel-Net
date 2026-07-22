using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Infrastructure.Persistence;
using CheckInApp.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CheckInApp.Tests.Infrastructure.Persistence.Repositories;

/// <summary>
/// Integration test (real Sqlite file, no mocks) proving the correctness property the
/// "process-booking" queue partitioner depends on: BookingOrderRepository.TryAssignRoom
/// must never let two concurrently-running requests both end up assigned to the SAME room.
///
/// This turns the one-time manual E2E check (4 concurrent requests, 3 rooms -> 3 Confirmed,
/// 1 Rejected, zero double-booking) into a permanent regression test. It intentionally uses
/// a real Sqlite file (not the in-memory provider) because the whole point is to exercise
/// Sqlite's actual cross-connection file locking / Serializable transaction behavior, which
/// a mocked repository or the in-memory provider cannot reproduce.
///
/// Only ONE room is seeded in the category. With 2 concurrent TryAssignRoom calls racing for
/// that single room, the only correct outcomes are:
///   - exactly one order Confirmed with that room's id, and
///   - the other order Rejected with RoomId == null
/// Both being Confirmed with the same RoomId would be a double-booking bug.
///
/// Sqlite (via the Serializable transaction in TryAssignRoom) may also legitimately surface
/// locking contention as a SqliteException (busy/locked) on one of the two concurrent
/// connections, rather than blocking and retrying automatically. Per the review, that is an
/// ACCEPTABLE outcome here too -- the property under test is "no double assignment / no data
/// corruption", not "both calls must always return without throwing". So a thrown
/// SqliteException on one side is tolerated as long as no double-assignment occurred.
/// </summary>
public class BookingOrderRepositoryConcurrencyTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public BookingOrderRepositoryConcurrencyTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"concurrency-test-{Guid.NewGuid()}.db");
        // Busy_Timeout gives Sqlite a generous window to wait on a lock held by the other
        // connection instead of immediately throwing SQLITE_BUSY, which keeps the test from
        // being flaky due to normal scheduling jitter between the two threads while still
        // allowing a genuine lock conflict under Serializable isolation to surface as an
        // exception (an outcome this test explicitly tolerates -- see class summary).
        _connectionString = $"Data Source={_dbPath};Cache=Shared;Foreign Keys=True;Pooling=False;Default Timeout=30";
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TryAssignRoom_TwoConcurrentOverlappingOrders_NeverAssignSameRoom()
    {
        // --- Arrange: seed 1 category + exactly 1 room, and 2 pending overlapping orders ---
        const int categoryId = 100;
        const int roomId = 1000;

        using (var seedContext = CreateContext())
        {
            seedContext.Database.EnsureCreated();

            seedContext.RoomCategories.Add(new RoomCategory
            {
                Id = categoryId,
                Name = "ConcurrencyTestCategory",
                MaxCapacity = 4,
                MinStayDays = 1,
                MaxStayDays = 14
            });

            seedContext.Rooms.Add(new Room
            {
                Id = roomId,
                Number = 9001,
                Status = RoomStatus.Available,
                DailyRate = 100m,
                MaxCapacity = 4,
                RoomCategoryId = categoryId
            });

            var checkIn = new DateTime(2026, 9, 1);
            var checkOut = new DateTime(2026, 9, 3);

            var orderA = new BookingOrder
            {
                IdempotencyKey = $"order-a-{Guid.NewGuid()}",
                Cpf = "111.111.111-11",
                GuestName = "Guest A",
                GuestCount = 1,
                RoomCategoryId = categoryId,
                RatePlanId = 1,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = BookingStatus.Pending,
                TotalPrice = 200m
            };

            var orderB = new BookingOrder
            {
                IdempotencyKey = $"order-b-{Guid.NewGuid()}",
                Cpf = "222.222.222-22",
                GuestName = "Guest B",
                GuestCount = 1,
                RoomCategoryId = categoryId,
                RatePlanId = 1,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = BookingStatus.Pending,
                TotalPrice = 200m
            };

            seedContext.BookingOrders.Add(orderA);
            seedContext.BookingOrders.Add(orderB);
            seedContext.SaveChanges();
        }

        // --- Act: two separate repositories, each on its OWN AppDbContext/connection,
        //     pointed at the same Sqlite file, racing to assign the single free room ---
        using var contextA = CreateContext();
        using var contextB = CreateContext();

        var repoA = new BookingOrderRepository(contextA);
        var repoB = new BookingOrderRepository(contextB);

        var orderAFromA = contextA.BookingOrders.Single(o => o.RoomCategoryId == categoryId && o.GuestName == "Guest A");
        var orderBFromB = contextB.BookingOrders.Single(o => o.RoomCategoryId == categoryId && o.GuestName == "Guest B");

        Exception? exceptionA = null;
        Exception? exceptionB = null;

        var taskA = Task.Run(() =>
        {
            try
            {
                repoA.TryAssignRoom(orderAFromA);
            }
            catch (SqliteException ex)
            {
                exceptionA = ex;
            }
        });

        var taskB = Task.Run(() =>
        {
            try
            {
                repoB.TryAssignRoom(orderBFromB);
            }
            catch (SqliteException ex)
            {
                exceptionB = ex;
            }
        });

        await Task.WhenAll(taskA, taskB);

        // --- Assert: reload fresh state from a third, untracked context/connection ---
        using var verifyContext = CreateContext();
        var finalOrders = verifyContext.BookingOrders
            .Where(o => o.RoomCategoryId == categoryId)
            .AsNoTracking()
            .ToList();

        finalOrders.Should().HaveCount(2);

        // Core property: no double-booking. At most one order may hold the single seeded
        // room, and no two Confirmed orders may share a RoomId.
        var confirmedOrders = finalOrders.Where(o => o.Status == BookingStatus.Confirmed).ToList();
        confirmedOrders.Should().HaveCountLessThanOrEqualTo(1,
            "only one room exists, so at most one order may be Confirmed against it");

        if (confirmedOrders.Count == 1)
        {
            confirmedOrders[0].RoomId.Should().Be(roomId);

            var rejectedOrders = finalOrders.Where(o => o.Status == BookingStatus.Rejected).ToList();
            rejectedOrders.Should().HaveCount(1);
            rejectedOrders[0].RoomId.Should().BeNull();
        }
        else
        {
            // Both sides threw a Sqlite locking exception before either committed a status
            // change -- acceptable per the documented Sqlite-locking caveat, since it still
            // proves no double-assignment occurred. At least one side must have surfaced an
            // exception to explain why neither order was processed.
            (exceptionA is not null || exceptionB is not null).Should().BeTrue(
                "if neither order was Confirmed, one of the two concurrent calls must have failed with a locking exception");
        }

        // No matter what, never allow the same room to be double-assigned: this is the
        // literal bug the partitioner + Serializable transaction exist to prevent.
        var roomIdsAssigned = finalOrders
            .Where(o => o.RoomId.HasValue)
            .Select(o => o.RoomId!.Value)
            .ToList();
        roomIdsAssigned.Distinct().Should().HaveCount(roomIdsAssigned.Count,
            "no two orders may ever be assigned the same RoomId");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // best-effort cleanup; a leftover temp file does not fail the test
            }
        }
    }
}
