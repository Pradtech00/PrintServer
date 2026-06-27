namespace OfficePrintServer.Core.Entities;

public class PrintJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PrinterId { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Printing, Completed, Failed
    public string ClientMachineName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}