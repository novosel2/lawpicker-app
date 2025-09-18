using System.IO.Compression;
using Application.Dto;
using Application.Exceptions;
using Application.Interfaces.IClients;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;

namespace Application.Services;

public class LawDocumentService : ILawDocumentService
{
    private readonly ILawDocumentClient _lawClient;
    private readonly ILawDocumentRepository _lawDocumentRepository;

    public LawDocumentService(ILawDocumentClient lawClient, ILawDocumentRepository lawDocumentRepository)
    {
        _lawClient = lawClient;
        _lawDocumentRepository = lawDocumentRepository;
    }


    public async Task ResetDatabaseAsync()
    {
        int limit = 1000;
        int offset = 0;
        int count = 0;

        int sum = 0;

        do 
        {
            Console.WriteLine($"Fetching {offset + 1} to {offset + limit}...");
            List<LawDocument> lawDocuments = await _lawClient.GetLawsAsync(limit, offset);
            var task = _lawDocumentRepository.AddChunkAsync(lawDocuments);
            count = lawDocuments.Count();
            await task;

            if (! await _lawDocumentRepository.IsSavedAsync())
                throw new Exception(); // make new exceptions later, im tired now

            sum += count;
            offset += limit;
        } while (count >= limit);
    }


    public async Task<LawDocumentsListResponse> GetLawDocumentsAsync(string? documentTypes, string? search, int page, int limit)
    {
        var lawDocuments = await _lawDocumentRepository.GetLawDocumentsAsync(documentTypes, search, page, limit);
        int count = await _lawDocumentRepository.GetLawDocumentsCountAsync(documentTypes, search);

        var lawDocumentsResponse = new LawDocumentsListResponse()
        {
            Count = count,
            LawDocuments = lawDocuments
        };

        return lawDocumentsResponse;
    }


    public async Task<byte[]> GetLawDocumentFilesAsync(List<string> celexNumbers, string lang)
    {
        using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var celex in celexNumbers)
                {
                    try
                    {
                        var fileBytes = await _lawClient.DownloadPdfAsync(celex, lang);
                        var entry = zip.CreateEntry($"document_{celex}.pdf");
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(fileBytes);
                    }
                    catch { /* Skip failed downloads */ }
                }
            }

        return zipStream.ToArray();
    }

    
    public async Task<string> GetUrlByCelexAsync(string celex, string lang = "EN")
    {
        var lawDocument = await _lawDocumentRepository.GetLawDocumentByCelexAsync(celex)
            ?? throw new NotFoundException($"Law document not found, CELEX: {celex}");

        string url = $"https://eur-lex.europa.eu/legal-content/{lang}/TXT/PDF/?uri=CELEX:{celex}";

        return url;
    }
}
