using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;

public interface IRatePlanRepository
{
    RatePlan[] ListByCategory(int roomCategoryId);
    RatePlan? GetById(int id);
}
