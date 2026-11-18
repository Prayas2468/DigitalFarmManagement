namespace DigitalFarmManagement.ViewModels;

public class ReportTableViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReportKey { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
    public List<string> ChartLabels { get; set; } = [];
    public List<decimal> ChartValues { get; set; } = [];
    public string ChartType { get; set; } = "none";
}

public class ReportHubItem
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
