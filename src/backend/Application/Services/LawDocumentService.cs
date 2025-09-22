using System.Collections.Concurrent;
using System.Diagnostics;
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
    private readonly ILawDocumentStorageService _storage;
    private readonly ILawDocumentRepository _lawDocumentRepository;
    private readonly ILogger<LawDocumentService> _logger;

    public LawDocumentService(ILawDocumentClient lawClient, ILawDocumentStorageService storage,
            ILawDocumentRepository lawDocumentRepository, ILogger<LawDocumentService> logger)
    {
        _lawClient = lawClient;
        _storage = storage;
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
                throw new SavingChangesFailedException("Failed while saving law documents in database."); 

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

    public async Task<Dictionary<string, string>> GetLawDocumentFilesAsync(List<string> celexNumbers, string lang)
    {
        _logger.LogDebug("Starting bulk download for {Count} documents", celexNumbers.Count);
        var overallSw = Stopwatch.StartNew();

        // Check cache status
        Dictionary<string, bool> cacheStatus = await _storage.CheckBulkExistenceAsync(celexNumbers, lang);
        ConcurrentDictionary<string, string> pdfUrls = new ConcurrentDictionary<string, string>();
        var cached = celexNumbers.Where(c => cacheStatus[c]).ToList();
        var notCached = celexNumbers.Where(c => !cacheStatus[c]).ToList();

        _logger.LogInformation("Cache status: {Cached} cached, {NotCached} need download",
            cached.Count, notCached.Count);

        var semaphore = new SemaphoreSlim(10);

        var downloadedCount = 0;
        var totalCount = celexNumbers.Count;

        var tasks = celexNumbers.Select(async celex =>
        {
            await semaphore.WaitAsync();
            try
            {
                string? pdfUrl = null;
                if (cacheStatus[celex])
                {
                    pdfUrl = await _storage.GetFromStorageAsync(celex, lang);
                }
                else
                {
                    Stream? pdfStream = await DownloadLawDocument(celex, lang);
                    if (pdfStream != null)
                    {
                        pdfUrl = await _storage.StoreDocumentAsync(celex, lang, pdfStream);
                    }
                }

                if (!string.IsNullOrEmpty(pdfUrl))
                {
                    pdfUrls[celex] = pdfUrl;
                    downloadedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {Celex}", celex);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed bulk download of {Total} documents in {Elapsed}ms",
            downloadedCount, overallSw.ElapsedMilliseconds);

        return pdfUrls.ToDictionary();
    }
    

    public async Task<string> GetUrlByCelexAsync(string celex, string lang = "EN")
    {
        var lawDocument = await _lawDocumentRepository.GetLawDocumentByCelexAsync(celex)
            ?? throw new NotFoundException($"Law document not found, CELEX: {celex}");

        string url = $"https://eur-lex.europa.eu/legal-content/{lang}/TXT/PDF/?uri=CELEX:{celex}";

        return url;
    }

    
    private async Task<Stream?> DownloadLawDocument(string celex, string lang)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var fileBytes = await _lawClient.DownloadPdfAsync(celex, lang);

            _logger.LogDebug("Document downloaded: {Celex} took {Elapsed}ms",
                    celex, sw.ElapsedMilliseconds);

            return fileBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {Celex}", celex);
            return null;
        }
    }
}
