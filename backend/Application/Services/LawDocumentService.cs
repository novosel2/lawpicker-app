using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Dto;
using Application.Exceptions;
using Application.Interfaces.IClients;
using Application.Interfaces.IRepositories;
using Application.Interfaces.IServices;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services;

public class LawDocumentService : ILawDocumentService
{
    private readonly ILawDocumentClient _lawClient;
    private readonly ILawDocumentStorageService _storage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILawDocumentRepository _lawDocumentRepository;
    private readonly ILogger<LawDocumentService> _logger;

    public LawDocumentService(ILawDocumentClient lawClient, ILawDocumentStorageService storage, IServiceProvider serviceProvider,
            ILawDocumentRepository lawDocumentRepository, ILogger<LawDocumentService> logger)
    {
        _lawClient = lawClient;
        _storage = storage;
        _serviceProvider = serviceProvider;
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

    public async Task<List<CelexUrlResponse>> GetLawDocumentFilesAsync(List<string> celexNumbers, string lang)
    {
        _logger.LogDebug("Starting bulk download for {Count} documents", celexNumbers.Count);
        var overallSw = Stopwatch.StartNew();

        // Check cache status
        Dictionary<string, bool> cacheStatus = await _storage.CheckBulkExistenceAsync(celexNumbers, lang);
        ConcurrentDictionary<string, string> pdfUrls = new ConcurrentDictionary<string, string>();
        List<CelexUrlResponse> celexResponses = [];
        var cached = celexNumbers.Where(c => cacheStatus[c]).ToList();
        var notCached = celexNumbers.Where(c => !cacheStatus[c]).ToList();

        _logger.LogInformation("Cache status: {Cached} cached, {NotCached} need download",
            cached.Count, notCached.Count);

        var downloadSemaphore = new SemaphoreSlim(10);
        var cacheSemaphore = new SemaphoreSlim(20);

        var downloadedCount = 0;
        var totalCount = celexNumbers.Count;

        var tasks = celexNumbers.Select(async celex =>
        {
            var celexUrlResponse = new CelexUrlResponse()
            {
                Celex = celex,
                RequestedLanguage = lang
            };
            string? pdfUrl = null;

            if (cacheStatus[celex])
            {
                await cacheSemaphore.WaitAsync();
                try
                {
                    pdfUrl = await _storage.GetFromStorageAsync(celex, lang);

                    if (pdfUrl == null)
                    {
                        cacheStatus[celex] = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process document {Celex}", celex);
                }
                finally
                {
                    cacheSemaphore.Release();
                }
            }

            if (!cacheStatus[celex])
            {
                await downloadSemaphore.WaitAsync();
                Stream? pdfStream = null;

                try
                {
                    pdfStream = await DownloadLawDocument(celex, lang);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process document {Celex}", celex);
                }
                finally
                {
                    downloadSemaphore.Release();
                }

                if (pdfStream == null)
                {
                    var availableLanguages = await CheckForAvailableLanguages(celex);

                    celexUrlResponse.Problem = "No translation for requested langauge.";
                    celexUrlResponse.AvailableLanguages = availableLanguages.Select(l => l.LanguageCode).ToList();
                }
                if (pdfStream != null)
                {
                    pdfUrl = await _storage.StoreDocumentAsync(celex, lang, pdfStream);
                }
            }

            if (!string.IsNullOrEmpty(pdfUrl))
            {
                celexUrlResponse.Url = pdfUrl;
                downloadedCount++;
            }

            celexResponses.Add(celexUrlResponse);
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed bulk download of {Total} documents in {Elapsed}ms",
            downloadedCount, overallSw.ElapsedMilliseconds);

        return celexResponses;
    }
    

    public async Task<CelexUrlResponse> GetUrlByCelexAsync(string celex, string lang = "EN")
    {
        var lawDocument = await _lawDocumentRepository.GetLawDocumentByCelexAsync(celex)
            ?? throw new NotFoundException($"Law document not found, CELEX: {celex}");

        var celexUrlResponse = new CelexUrlResponse()
        {
            Celex = celex,
            RequestedLanguage = lang
        };

        string? url = "";

        if(! await _storage.ExistsInCacheAsync(celex, lang))
        {
            Stream? pdf = await _lawClient.DownloadPdfAsync(celex, lang);

            if (pdf == null)
            {
                var availableLanguages = await CheckForAvailableLanguages(celex);

                celexUrlResponse.Problem = "No translation for requested langauge.";
                celexUrlResponse.AvailableLanguages = availableLanguages.Select(l => l.LanguageCode).ToList();

                return celexUrlResponse;
            }

            url = await _storage.StoreDocumentAsync(celex, lang, pdf);
        }

        url = await _storage.GetFromStorageAsync(celex, lang);

        celexUrlResponse.Url = url;

        return celexUrlResponse;
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


    private async Task<List<Language>> CheckForAvailableLanguages(string celex)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILawDocumentRepository>();

        List<Language> languages = await repository.GetAllLanguagesAsync();
        List<Language> availableLanguages = [];

        foreach (var language in languages)
        {
            _logger.LogInformation($"Language: {language.LanguageCode}");
            var pdf = await DownloadLawDocument(celex, language.LanguageCode);
            _logger.LogInformation(pdf?.ToString());

            if (pdf != null)
            {
                availableLanguages.Add(language);
            }
        }

        return availableLanguages;
    }
}
