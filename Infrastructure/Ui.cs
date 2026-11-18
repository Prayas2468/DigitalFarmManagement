using DigitalFarmManagement.Models;

namespace DigitalFarmManagement.Infrastructure;

public static class Ui
{
    public static string Pill(string? status) => status switch
    {
        MrlResults.Compliant or WithdrawalStatuses.Completed or HealthStatuses.Healthy => "pill pill-ok",
        MrlResults.NonCompliant or WithdrawalStatuses.Active or HealthStatuses.Critical => "pill pill-bad",
        MrlResults.Pending or WithdrawalStatuses.EndingSoon or HealthStatuses.UnderTreatment or HealthStatuses.Recovering => "pill pill-wait",
        _ => "pill"
    };

    public static string When(DateTime value) => value.ToString("dd MMM yyyy");

    public static string Qty(decimal value) => value.ToString("0.###");

    public static string Measure(decimal value) => value.ToString("0.####");

    public static string Ago(DateTime value)
    {
        var span = DateTime.Now - value;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} hr ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
        return value.ToString("dd MMM");
    }
}
