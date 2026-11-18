namespace DigitalFarmManagement.ViewModels;

public class PagerViewModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Index";
    public Dictionary<string, string> Routes { get; set; } = new();
}

public class NavAlertsViewModel
{
    public int Unread { get; set; }
    public List<Models.Notification> Latest { get; set; } = [];
}
