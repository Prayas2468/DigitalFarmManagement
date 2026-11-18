namespace DigitalFarmManagement.Infrastructure;

/// <summary>
/// Withdrawal dates are calculated here so every screen uses the same rule.
/// The first day a product may be sold is the treatment end date plus the
/// medicine's withdrawal period. Example: treatment ends on 1 January and the
/// withdrawal period is 7 days, so the product is eligible on 8 January.
/// </summary>
public static class WithdrawalMath
{
    public static DateTime FirstEligibleDay(DateTime treatmentEnd, int withdrawalDays)
        => treatmentEnd.Date.AddDays(Math.Max(0, withdrawalDays));

    public static int RemainingDays(DateTime firstEligibleDay, DateTime today)
    {
        var days = (firstEligibleDay.Date - today.Date).Days;
        return days < 0 ? 0 : days;
    }

    public static bool BlocksSale(DateTime firstEligibleDay, DateTime today)
        => today.Date < firstEligibleDay.Date;

    public static string Status(DateTime firstEligibleDay, DateTime today)
    {
        var remaining = (firstEligibleDay.Date - today.Date).Days;
        if (remaining <= 0)
            return Models.WithdrawalStatuses.Completed;
        if (remaining <= 3)
            return Models.WithdrawalStatuses.EndingSoon;
        return Models.WithdrawalStatuses.Active;
    }
}
