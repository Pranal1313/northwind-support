using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Configuration;

/// <summary>
/// Configurable SLA windows and thresholds, bound from the Sla configuration section.
/// </summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<string, double> BaseWindowHours { get; set; } = new()
    {
        [nameof(TicketPriority.Low)] = 72,
        [nameof(TicketPriority.Medium)] = 24,
        [nameof(TicketPriority.High)] = 8,
        [nameof(TicketPriority.Critical)] = 4
    };

    public double PremiumCustomerMultiplier { get; set; } = 0.5;

    public double MinimumWindowHours { get; set; } = 1;

    public double AtRiskThresholdPercent { get; set; } = 25;
}