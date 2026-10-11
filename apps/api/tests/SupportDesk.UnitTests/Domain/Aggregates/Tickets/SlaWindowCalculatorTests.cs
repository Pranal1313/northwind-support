using SupportDesk.Application.Configuration;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaWindowCalculatorTests
{
    private readonly SlaWindowCalculator _calculator =
        new(new SlaOptions());

    [Theory]
    [InlineData(TicketPriority.Critical, 4)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.Low, 72)]
    public void Calculate_StandardCustomer_UsesBaseWindow(
        TicketPriority priority,
        double expectedHours)
    {
        var result = _calculator.Calculate(priority, CustomerTier.Standard);

        Assert.Equal(TimeSpan.FromHours(expectedHours), result);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, 2)]
    [InlineData(TicketPriority.High, 4)]
    [InlineData(TicketPriority.Medium, 12)]
    [InlineData(TicketPriority.Low, 36)]
    public void Calculate_PremiumCustomer_HalvesBaseWindow(
        TicketPriority priority,
        double expectedHours)
    {
        var result = _calculator.Calculate(priority, CustomerTier.Premium);

        Assert.Equal(TimeSpan.FromHours(expectedHours), result);
    }

    [Fact]
    public void Calculate_PremiumCustomer_NeverGoesBelowOneHour()
    {
        var options = new SlaOptions
        {
            BaseWindowHours = new()
            {
                [nameof(TicketPriority.Critical)] = 1
            },
            PremiumCustomerMultiplier = 0.5,
            MinimumWindowHours = 1
        };

        var calculator = new SlaWindowCalculator(options);

        var result = calculator.Calculate(
            TicketPriority.Critical,
            CustomerTier.Premium);

        Assert.Equal(TimeSpan.FromHours(1), result);
    }
}