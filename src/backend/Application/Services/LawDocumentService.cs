using System.Diagnostics;
using System.IO.Compression;
using Application.Dto;
using Application.Exceptions;
using Application.Interfaces.IClients;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class LawDocumentService : ILawDocumentService
{
    private readonly ILawDocumentClient _lawClient;
    private readonly ILawDocumentRepository _lawDocumentRepository;
    private readonly ILogger<LawDocumentService> _logger;

    public LawDocumentService(ILawDocumentClient lawClient, ILawDocumentRepository lawDocumentRepository, 
            ILogger<LawDocumentService> logger)
    {
        _lawClient = lawClient;
        _lawDocumentRepository = lawDocumentRepository;
        _logger = logger;
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
        _logger.LogInformation("Entering GetLawDocumentFilesAsync method in LawDocumentService.");
        var swMain = Stopwatch.StartNew();

        var results = new List<DownloadResult>();

        int maxConcurrency = 10;
        var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = celexNumbers.Select(async celex => 
        {
            await semaphore.WaitAsync();
            try
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Downloading PDF {Celex}", celex);

                var fileBytes = await _lawClient.DownloadPdfAsync(celex, lang);

                sw.Stop();
                _logger.LogInformation("Downloaded PDF {Celex} in {Elapsed}ms", celex, sw.ElapsedMilliseconds);

                return new DownloadResult()
                {
                    Celex = celex,
                    Data = fileBytes,
                    Success = true
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download {Celex}", celex);
                return new DownloadResult()
                {
                    Celex = celex,
                    Data = new byte[0],
                    Success = false
                };
            }
            finally 
            {
                semaphore.Release();
            }
        });

        var files = await Task.WhenAll(tasks);

        using var zipStream = new MemoryStream();
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true);

        foreach (var file in files)
        {
            _logger.LogInformation("Started writing {Celex} to zip", file.Celex);
            var sw = Stopwatch.StartNew();

            var entry = zip.CreateEntry($"document_{file.Celex}.pdf", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(file.Data);

            sw.Stop();
            _logger.LogInformation("Finished writing {Celex} to zip", file.Celex);

        }
        swMain.Stop();
        _logger.LogInformation("All downloads and zipping finished in {Elapsed}ms", swMain.ElapsedMilliseconds);

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
