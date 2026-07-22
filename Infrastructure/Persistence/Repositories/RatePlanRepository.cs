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
