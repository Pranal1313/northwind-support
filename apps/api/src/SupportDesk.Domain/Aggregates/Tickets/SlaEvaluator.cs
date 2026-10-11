namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        DateTime? createdAtUtc = null,
        double atRiskThresholdPercent = 25)
    {
        if (dueAtUtc is null)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is not null)
        {
            return resolvedAtUtc <= dueAtUtc
                ? SlaStatus.Met
                : SlaStatus.Breached;
        }

        if (nowUtc > dueAtUtc)
        {
            return SlaStatus.Breached;
        }

        if (createdAtUtc is not null)
        {
            var totalWindow = dueAtUtc.Value - createdAtUtc.Value;

            if (totalWindow > TimeSpan.Zero)
            {
                var remainingWindow = dueAtUtc.Value - nowUtc;
                var remainingPercent = remainingWindow.TotalSeconds
                    / totalWindow.TotalSeconds * 100;

                if (remainingPercent <= atRiskThresholdPercent)
                {
                    return SlaStatus.AtRisk;
                }
            }
        }

        return SlaStatus.WithinSla;
    }
}