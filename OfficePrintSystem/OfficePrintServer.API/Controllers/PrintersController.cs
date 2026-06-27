using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;
    private readonly IPrintRepository _repository;

    public PrintersController(IPrinterService printerService, IPrintRepository repository)
    {
        _printerService = printerService;
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Printer>>> Get()
    {
        var dbPrinters = await _repository.GetPrintersAsync();
        return Ok(dbPrinters);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var installed = await _printerService.GetInstalledPrintersAsync();
        var existing = await _repository.GetPrintersAsync();
        var existingDict = new Dictionary<string, Printer>();
        foreach (var p in existing)
        {
            existingDict[p.SystemName] = p;
        }

        foreach (var p in installed)
        {
            if (!existingDict.ContainsKey(p.SystemName))
            {
                await _repository.AddPrinterAsync(p);
            }
            else
            {
                var existingPrinter = existingDict[p.SystemName];
                await _repository.UpdatePrinterStatusAsync(existingPrinter.Id, p.Status);
            }
        }

        return Ok(new { Message = "Printers list refreshed successfully." });
    }
}