using System.Data;
using Microsoft.EntityFrameworkCore;
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
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);

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
