using Microsoft.AspNetCore.Mvc;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;
using OfficePrintServer.Infrastructure.Services;

namespace OfficePrintServer.API.Controllers;

[ApiController]
[Route("ipp/{printerName?}")]
public class IppController : ControllerBase
{
    private readonly IPrintRepository _repository;
    private readonly IPrinterService _printerService;
    private readonly ILogger<IppController> _logger;

    public IppController(IPrintRepository repository, IPrinterService printerService, ILogger<IppController> logger)
    {
        _repository = repository;
        _printerService = printerService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleIpp(string? printerName = null)
    {
        try
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            byte[] ippData = ms.ToArray();

            if (ippData.Length == 0)
                return BadRequest("Empty IPP request");

            _logger.LogInformation("IPP POST received. Length: {Len}, First bytes: {Bytes}",
                ippData.Length,
                string.Join(" ", ippData.Take(Math.Min(16, ippData.Length)).Select(b => b.ToString("X2"))));

            var parsed = IppParser.Parse(ippData, printerName);

            _logger.LogInformation("IPP parsed: Version={Ver}, OpId={OpId}, ReqId={ReqId}, Printer={Printer}",
                parsed.Version, parsed.OperationId, parsed.RequestId, parsed.PrinterName);

            if (parsed.OperationId == 0x000b)
            {
                _logger.LogInformation("IPP Get-Printer-Attributes for printer: {Printer}", parsed.PrinterName);
                var response = IppParser.BuildGetPrinterAttributesResponse(parsed.PrinterName);
                return File(response, "application/ipp");
            }

            if (parsed.OperationId == 0x0004 || parsed.OperationId == 0x0005)
            {
                // Validate-Job or Create-Job - respond success
                _logger.LogInformation("IPP Validate/Create-Job for printer: {Printer}", parsed.PrinterName);
                var vResponse = IppParser.BuildValidateJobResponse();
                return File(vResponse, "application/ipp");
            }

            if (parsed.OperationId == 0x0002)
            {
                _logger.LogInformation("IPP Print-Job received. Printer: {Printer}, Document: {Doc}",
                    parsed.PrinterName, parsed.JobName);

                if (!parsed.HasDocument)
                    return BadRequest("No document data in IPP request");

                var extension = ".pdf";
                string? detectedExtension = null;
                if (parsed.JobName.Contains('.'))
                    detectedExtension = Path.GetExtension(parsed.JobName);

                var fileName = $"{Guid.NewGuid()}{detectedExtension ?? extension}";
                var tempDir = Path.Combine(Path.GetTempPath(), "OfficePrintServerTemp");
                Directory.CreateDirectory(tempDir);
                var filePath = Path.Combine(tempDir, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, parsed.DocumentData);

                var printers = await _repository.GetPrintersAsync();
                var printer = printers.FirstOrDefault(p =>
                    p.SystemName.Equals(parsed.PrinterName, StringComparison.OrdinalIgnoreCase));

                if (printer == null)
                {
                    _logger.LogWarning("Printer not found in DB, creating entry: {Printer}", parsed.PrinterName);
                    printer = new Printer
                    {
                        SystemName = parsed.PrinterName,
                        DisplayName = parsed.PrinterName,
                        IsActive = true,
                        Status = "Ready"
                    };
                    await _repository.AddPrinterAsync(printer);
                }

                var job = new PrintJob
                {
                    PrinterId = printer.Id,
                    DocumentName = parsed.JobName, // original document name parsed from IPP
                    FilePath = filePath,
                    Status = "Spooled",
                    ClientMachineName = Request.Headers["User-Agent"].ToString()
                };

                await _repository.AddJobAsync(job);

                // Print langsung ke Windows Spooler lokal (driver printer menangani antrean)
                try
                {
                    await _printerService.PrintPdfAsync(printer.SystemName, filePath, parsed.JobName);
                    await _repository.UpdateJobStatusAsync(job.Id, "Completed");
                    _logger.LogInformation("Job {JobId} completed via Windows Spooler.", job.Id);
                }
                catch (Exception ex)
                {
                    await _repository.UpdateJobStatusAsync(job.Id, "Failed", ex.Message);
                    _logger.LogError(ex, "Job {JobId} failed during printing.", job.Id);
                }

                var ippResponse = IppParser.BuildPrintJobResponse();
                return File(ippResponse, "application/ipp");
            }

            _logger.LogWarning("Unsupported IPP operation: {OpId}", parsed.OperationId);
            return BadRequest($"Unsupported IPP operation: {parsed.OperationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling IPP request");
            return StatusCode(500);
        }
    }

    [HttpGet]
    public IActionResult Get(string? printerName = "IPP Printer")
    {
        var name = string.IsNullOrEmpty(printerName) ? "IPP Printer" : printerName;
        var response = IppParser.BuildGetPrinterAttributesResponse(name);
        return File(response, "application/ipp");
    }
}
