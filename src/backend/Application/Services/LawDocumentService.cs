using System.Collections.Concurrent;
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
    public async Task GetLawDocumentFilesAsync(List<string> celexNumbers, string lang, ZipArchive zip)
    {
        _logger.LogDebug("Starting bulk download for {Count} documents", celexNumbers.Count);
        var overallSw = Stopwatch.StartNew();
        
        // Check cache status
        var cacheStatus = await _storage.CheckBulkExistenceAsync(celexNumbers);
        var cached = celexNumbers.Where(c => cacheStatus[c]).ToList();
        var notCached = celexNumbers.Where(c => !cacheStatus[c]).ToList();
        
        _logger.LogInformation("Cache status: {Cached} cached, {NotCached} need download", 
            cached.Count, notCached.Count);
        
        // Step 1: Download all missing documents in parallel
        var downloadedFiles = new ConcurrentDictionary<string, Stream>();
        var downloadSemaphore = new SemaphoreSlim(5); // Rate limit EUR-Lex
        var cacheSemaphore = new SemaphoreSlim(20);
        
        var downloadTasks = notCached.Select(async celex =>
        {
            await downloadSemaphore.WaitAsync();
            try
            {
                using var stream = await DownloadLawDocument(celex, lang);
                if (stream != null)
                {
                    var sw = Stopwatch.StartNew();

                    // Copy to memory so the stream is usable after disposal
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;

                    // Store a copy in memory for later use
                    downloadedFiles[celex] = new MemoryStream(ms.ToArray());

                    // Store to storage as well
                    ms.Position = 0;
                    await _storage.StoreDocumentAsync(celex, ms);

                    _logger.LogDebug("{Celex} writing to memory took {Elapsed}ms", celex, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download {Celex}", celex);
            }
            finally
            {
                downloadSemaphore.Release();
            }
        });
        
        await Task.WhenAll(downloadTasks);
        

        var cacheTasks = cached.Select(async celex =>
        {
            await cacheSemaphore.WaitAsync();
            try
            {
                using var stream = await _storage.GetFromStorageAsync(celex);
                if (stream != null)
                {
                    var sw = Stopwatch.StartNew();

                    // Copy to memory so the stream is usable after disposal
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;

                    // Store a copy in memory for later use
                    downloadedFiles[celex] = new MemoryStream(ms.ToArray());

                    _logger.LogDebug("{Celex} writing to memory took {Elapsed}ms", celex, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add cached document {Celex}", celex);
            }
            finally
            {
                cacheSemaphore.Release();
            }
        });
        
        await Task.WhenAll(cacheTasks);
        
        // Then add newly downloaded documents
        foreach (var kvp in downloadedFiles)
        {
            _logger.LogDebug("Writing {Celex} to zip", kvp.Key);

            var entry = zip.CreateEntry($"document_{kvp.Key}.pdf", CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            await kvp.Value.CopyToAsync(entryStream);
        }
        
        var totalSuccess = downloadedFiles.Count;
        _logger.LogInformation("Completed bulk download: {Success}/{Total} documents in {Elapsed}ms",
            totalSuccess, celexNumbers.Count, overallSw.ElapsedMilliseconds);
    }
    // public async Task GetLawDocumentFilesAsync(List<string> celexNumbers, string lang, ZipArchive zip)
    // {
    //     _logger.LogDebug("Starting bulk download for {Count} documents", celexNumbers.Count);
    //     var overallSw = Stopwatch.StartNew();
    //
    //     var tempFiles = new List<(string Celex, string TempFile)>();
    //     var downloadTasks = new List<Task>();
    //     int successCount = 0;
    //     int maxConcurrency = 10;
    //     var semaphore = new SemaphoreSlim(maxConcurrency);
    //
    //     foreach (var celex in celexNumbers)
    //     {
    //         await semaphore.WaitAsync();
    //
    //         if (await _storage.ExistsInCacheAsync(celex))
    //         {
    //             var sw = Stopwatch.StartNew();
    //             _logger.LogDebug("Pulling {Celex} from cache", celex);
    //
    //             var entry = zip.CreateEntry($"document_{celex}.pdf", CompressionLevel.Fastest);
    //             
    //             var fileStream = await _storage.GetFromStorageAsync(celex);
    //             if (fileStream != null)
    //             {
    //                 using var entryStream = entry.Open();
    //                 await fileStream.CopyToAsync(entryStream);
    //
    //                 _logger.LogDebug("{Celex} pulled from storage in {Elapsed}ms", celex, sw.ElapsedMilliseconds);
    //             }
    //
    //             semaphore.Release();
    //             continue;
    //         }
    //
    //         downloadTasks.Add(Task.Run(async () => 
    //         {
    //             var tempFile = Path.GetTempFileName();
    //             using var documentStream = await DownloadLawDocument(celex, lang);
    //
    //             if (documentStream != null)
    //             {
    //                 using var fileStream = File.Create(tempFile);
    //                 await documentStream.CopyToAsync(fileStream);
    //
    //                 lock (tempFiles)
    //                     tempFiles.Add((celex, tempFile));
    //
    //                 await _storage.StoreDocumentAsync(celex, documentStream);
    //
    //                 successCount++;
    //             }
    //         }));
    //
    //         semaphore.Release();
    //     }
    //
    //     await Task.WhenAll(downloadTasks);
    //
    //     try
    //     {
    //         foreach (var (celex, tempFile) in tempFiles)
    //         {
    //             var entry = zip.CreateEntry($"document_{celex}.pdf", CompressionLevel.Fastest);
    //             
    //             using var fileStream = File.OpenRead(tempFile);
    //             using var entryStream = entry.Open();
    //             await fileStream.CopyToAsync(entryStream);
    //         }
    //     }
    //     finally
    //     {
    //         foreach (var (_, tempFile) in tempFiles)
    //             try { File.Delete(tempFile); } catch { }
    //     }
    //
    //     if (successCount < celexNumbers.Count())
    //     {
    //         _logger.LogWarning("Downloaded {Success}/{Total} documents successfully",
    //                 successCount, celexNumbers.Count());
    //     }
    //
    //     _logger.LogDebug("Completed bulk download: {Count} files in {Elapsed}ms",
    //             successCount, overallSw.ElapsedMilliseconds);
    // }

    
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
            _logger.LogDebug("Downloading document {Celex}", celex);

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
