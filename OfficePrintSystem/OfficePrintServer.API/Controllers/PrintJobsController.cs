using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintJobsController : ControllerBase
{
    private readonly IPrintRepository _repository;
    private readonly IPrintQueue _queue;

    public PrintJobsController(IPrintRepository repository, IPrintQueue queue)
    {
        _repository = repository;
        _queue = queue;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrintJob>>> Get()
    {
        var jobs = await _repository.GetJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<PrintJob>> GetStatus(string id)
    {
        var job = await _repository.GetJobByIdAsync(id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] string printerId, [FromForm] string documentName, [FromForm] string clientMachine, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var printer = await _repository.GetPrinterByIdAsync(printerId);
        if (printer == null)
        {
            return BadRequest("Invalid Printer ID.");
        }

        // Save file to temp folder on server
        var tempFolder = Path.Combine(Path.GetTempPath(), "OfficePrintServerTemp");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var jobId = Guid.NewGuid().ToString();
        var filePath = Path.Combine(tempFolder, $"{jobId}_{file.FileName}");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var job = new PrintJob
        {
            Id = jobId,
            PrinterId = printerId,
            DocumentName = documentName,
            FilePath = filePath,
            Status = "Pending",
            ClientMachineName = clientMachine,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddJobAsync(job);
        await _queue.EnqueueJobAsync(job);

        return Ok(new { JobId = jobId, Status = job.Status });
    }
}