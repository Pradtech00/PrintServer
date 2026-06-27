namespace OfficePrintServer.Core.Entities;

public class Printer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SystemName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Ready";
    public bool IsActive { get; set; } = true;
}