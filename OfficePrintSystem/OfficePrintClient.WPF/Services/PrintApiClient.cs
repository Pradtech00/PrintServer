using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using OfficePrintClient.WPF.Models;

namespace OfficePrintClient.WPF.Services;

public class PrintApiClient
{
    private HttpClient _client;

    public PrintApiClient()
    {
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    public bool IsConnected => _client.BaseAddress != null;

    public void SetServerAddress(string ipAddress)
    {
        var uri = ipAddress.TrimEnd('/');
        if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
            uri = $"http://{uri}";

        if (!uri.Contains(':'))
            uri = $"{uri}:18080";

        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
        _client.BaseAddress = new Uri(uri);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _client.GetAsync("/api/printers");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PrinterInfo>> GetPrintersAsync()
    {
        try
        {
            var result = await _client.GetFromJsonAsync<List<PrinterInfo>>("/api/printers");
            return result ?? new List<PrinterInfo>();
        }
        catch
        {
            return new List<PrinterInfo>();
        }
    }

    public async Task<bool> RefreshPrintersAsync()
    {
        try
        {
            var response = await _client.PostAsync("/api/printers/refresh", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<JobCreateResponse?> SendPrintJobAsync(string printerId, string filePath)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(printerId), "printerId");
            form.Add(new StringContent(Path.GetFileName(filePath)), "documentName");
            form.Add(new StringContent(Environment.MachineName), "clientMachine");

            var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _client.PostAsync("/api/printjobs", form);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<JobCreateResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PrintJobInfo>> GetJobsAsync()
    {
        try
        {
            var result = await _client.GetFromJsonAsync<List<PrintJobInfo>>("/api/printjobs");
            return result ?? new List<PrintJobInfo>();
        }
        catch
        {
            return new List<PrintJobInfo>();
        }
    }
}
