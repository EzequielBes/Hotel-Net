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
