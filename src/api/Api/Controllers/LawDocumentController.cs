using Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("/api/laws")]
public class LawDocumentController : ControllerBase
{
    private readonly ILawDocumentService _lawDocumentService;

    public LawDocumentController(ILawDocumentService lawDocumentService)
    {
        _lawDocumentService = lawDocumentService;
    }
    

    [HttpGet]
    public async Task<IActionResult> GetLawDocuments(string? documentTypes, string? search, int page = 0, int limit = 10)
    {
        var lawDocuments = await _lawDocumentService.GetLawDocumentsAsync(documentTypes, search, page, limit);
        return Ok(lawDocuments);
    }


    [HttpPost("pdf")]
    public async Task<IActionResult> GetLawDocumentFiles(List<string> celexNumbers, string lang = "EN")
    {
        var pdfBytes = await _lawDocumentService.GetLawDocumentFilesAsync(celexNumbers, lang);
        return File(pdfBytes, "application/zip", "PDFs.zip");
    }


    [HttpGet("{celex}/pdf")]
    public async Task<IActionResult> GetLawDocumentByCelex(string celex, string lang = "EN")
    {
        var lawDocumentUrl = await _lawDocumentService.GetUrlByCelexAsync(celex, lang);
        return Ok(new { url = lawDocumentUrl });
    }


    [HttpPost("fill-database")]
    public async Task<IActionResult> StoreAllLaws()
    {
        await _lawDocumentService.ResetDatabaseAsync();
        return Ok("Everything saved successfully!");
    }
}
