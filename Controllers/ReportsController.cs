using DigitalFarmManagement.Infrastructure;
using DigitalFarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalFarmManagement.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports)
    {
        _reports = reports;
    }

    public IActionResult Index() => View(ReportService.Hub);

    public Task<IActionResult> ViewReport(string key, DateTime? from, DateTime? to, string? search)
        => Show(key, from, to, search);

    public async Task<IActionResult> Csv(string key, DateTime? from, DateTime? to, string? search)
    {
        var report = await _reports.BuildAsync(key, from, to, search);
        if (report == null)
            return NotFound();
        var bytes = CsvBuilder.Build(report.Headers, Rows(report.Rows));
        return File(bytes, "text/csv", $"{key}.csv");
    }

    public async Task<IActionResult> Pdf(string key, DateTime? from, DateTime? to, string? search)
    {
        var report = await _reports.BuildAsync(key, from, to, search);
        if (report == null)
            return NotFound();
        var subtitle = $"{report.Description}  ·  Printed {DateTime.Now:dd MMM yyyy HH:mm}";
        var bytes = PdfReportBuilder.Build(report.Title, subtitle, report.Headers, Rows(report.Rows));
        return File(bytes, "application/pdf", $"{key}.pdf");
    }

    private async Task<IActionResult> Show(string key, DateTime? from, DateTime? to, string? search)
    {
        var report = await _reports.BuildAsync(key, from, to, search);
        return report == null ? NotFound() : View("Report", report);
    }

    private static List<IReadOnlyList<string>> Rows(List<List<string>> rows)
        => rows.Select(row => (IReadOnlyList<string>)row).ToList();
}
