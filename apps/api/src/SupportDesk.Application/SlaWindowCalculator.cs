using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Services;

/// <summary>
/// Calculates an SLA duration from the configured priority window and customer tier.
/// </summary>
public sealed class SlaWindowCalculator(SlaOptions options)
{
    public TimeSpan Calculate(TicketPriority priority, CustomerTier customerTier)
    {
        if (!options.BaseWindowHours.TryGetValue(priority.ToString(), out var baseHours))
        {
            throw new InvalidOperationException(
                $"No SLA window is configured for priority '{priority}'.");
        }

        if (baseHours <= 0 || options.MinimumWindowHours <= 0)
        {
            throw new InvalidOperationException(
                "SLA window hours and the minimum window must be greater than zero.");
        }

        var hours = customerTier == CustomerTier.Premium
            ? Math.Max(
                baseHours * options.PremiumCustomerMultiplier,
                options.MinimumWindowHours)
            : baseHours;

        return TimeSpan.FromHours(hours);
    }
}