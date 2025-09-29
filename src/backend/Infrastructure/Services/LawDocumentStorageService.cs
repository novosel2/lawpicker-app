using System.Diagnostics;
using Application.Interfaces.IServices;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class LawDocumentStorageService : ILawDocumentStorageService
{
    private readonly BlobContainerClient? _blobContainer;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LawDocumentStorageService> _logger;
    private readonly string _containerName;
    private readonly bool _useAzureStorage;
    private readonly string? _localStoragePath;
    private readonly string _baseUrl;

    public LawDocumentStorageService(IConfiguration configuration, ILogger<LawDocumentStorageService> logger)
    {
        _logger = logger;
        _containerName = configuration["AzureStorage:LawContainer"] ?? "law-documents";

        // Check if Azure Storage is configured
        var azureConnectionString = configuration.GetConnectionString("AzureStorage");
        _useAzureStorage = !string.IsNullOrWhiteSpace(azureConnectionString);
        _baseUrl = configuration["LocalStorage:BaseUrl"] ?? "http://localhost:8000"; 

        if (_useAzureStorage)
        {
            // Use Azure Blob Storage
            var blobServiceClient = new BlobServiceClient(azureConnectionString);
            _blobContainer = blobServiceClient.GetBlobContainerClient(_containerName);
            _blobContainer.CreateIfNotExists();
            _logger.LogInformation("Using Azure Blob Storage for document storage");
        }
        else
        {
            // Use Local File Storage
            _localStoragePath = configuration["LocalStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage", "LawDocuments");
            
            Directory.CreateDirectory(_localStoragePath);
            _logger.LogInformation("Azure Storage not configured. Using local file storage at: {Path}", _localStoragePath);
        }

        // Redis is always used for caching URLs
        var redisConnectionString = configuration.GetConnectionString("Redis");
        _redis = ConnectionMultiplexer.Connect(redisConnectionString!);
    }

    public async Task<bool> ExistsInCacheAsync(string celexNumber, string lang)
    {
        var db = _redis.GetDatabase();
        string key = $"doc:{celexNumber}_{lang}";

        // First check Redis cache
        bool exists = await db.KeyExistsAsync(key);
        
        if (!exists)
        {
            // Check actual storage (Azure or Local)
            bool fileExists = _useAzureStorage 
                ? await CheckAzureStorageAsync(celexNumber, lang)
                : CheckLocalStorage(celexNumber, lang);

            if (!fileExists)
            {
                _logger.LogWarning("Document {Celex}_{Lang} not found in storage", celexNumber, lang);
                return false;
            }

            // File exists but not in Redis, generate URL and cache it
            await GenerateAndCacheUrl(celexNumber, lang);
        }

        return true;
    }

    public async Task<string?> GetFromStorageAsync(string celexNumber, string lang)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var db = _redis.GetDatabase();
            string key = $"doc:{celexNumber}_{lang}";

            // Check if file exists in storage
            bool fileExists = _useAzureStorage 
                ? await CheckAzureStorageAsync(celexNumber, lang)
                : CheckLocalStorage(celexNumber, lang);

            if (!fileExists)
            {
                _logger.LogWarning("Document {Celex}_{Lang} not found in storage", celexNumber, lang);
                return null;
            }

            // Try to get URL from Redis cache
            string? url = await db.StringGetAsync(key);
            var ttl = await db.KeyTimeToLiveAsync(key);

            // Regenerate URL if not in cache or expiring soon (within 1 day)
            if (string.IsNullOrEmpty(url) || (ttl.HasValue && ttl.Value.TotalDays <= 1))
            {
                url = await GenerateAndCacheUrl(celexNumber, lang);
            }

            _logger.LogDebug("{Celex}_{Lang} retrieved in {Elapsed}ms", celexNumber, lang, sw.ElapsedMilliseconds);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {Celex}_{Lang} from storage", celexNumber, lang);
            return null;
        }
    }

    public async Task<string?> StoreDocumentAsync(string celexNumber, string lang, Stream content)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            content.Position = 0;

            string url;
            if (_useAzureStorage)
            {
                // Store in Azure Blob Storage
                var blobClient = _blobContainer!.GetBlobClient($"{celexNumber}_{lang}.pdf");
                await blobClient.UploadAsync(content, overwrite: true);
                
                // Generate SAS URL
                url = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7)).ToString();
            }
            else
            {
                // Store in Local File System
                var fileName = $"{celexNumber}_{lang}.pdf";
                var filePath = Path.Combine(_localStoragePath!, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await content.CopyToAsync(fileStream);
                }

                // Generate local URL (you'll need to serve these files via static file middleware)
                url = $"{_baseUrl}/storage/law-documents/{fileName}";
            }

            // Cache URL in Redis
            string key = $"doc:{celexNumber}_{lang}";
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, url, TimeSpan.FromDays(7));

            _logger.LogInformation("Document stored: {Celex}_{Lang} took {Elapsed}ms", 
                celexNumber, lang, sw.ElapsedMilliseconds);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document {Celex}_{Lang}", celexNumber, lang);
            return null;
        }
    }

    public async Task<Dictionary<string, bool>> CheckBulkExistenceAsync(List<string> celexNumbers, string lang)
    {
        var results = new Dictionary<string, bool>();
        
        foreach (var celex in celexNumbers)
        {
            results[celex] = await ExistsInCacheAsync(celex, lang);
        }
        
        return results;
    }

    // Helper methods
    private async Task<bool> CheckAzureStorageAsync(string celexNumber, string lang)
    {
        if (_blobContainer == null) return false;
        
        var blobClient = _blobContainer.GetBlobClient($"{celexNumber}_{lang}.pdf");
        return await blobClient.ExistsAsync();
    }

    private bool CheckLocalStorage(string celexNumber, string lang)
    {
        if (string.IsNullOrEmpty(_localStoragePath)) return false;
        
        var filePath = Path.Combine(_localStoragePath, $"{celexNumber}_{lang}.pdf");
        return File.Exists(filePath);
    }

    private async Task<string?> GenerateAndCacheUrl(string celexNumber, string lang)
    {
        string url;
        
        if (_useAzureStorage)
        {
            var blobClient = _blobContainer!.GetBlobClient($"{celexNumber}_{lang}.pdf");
            url = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7)).ToString();
        }
        else
        {
            url = $"{_baseUrl}/storage/law-documents/{celexNumber}_{lang}.pdf";
        }

        // Cache in Redis
        var db = _redis.GetDatabase();
        string key = $"doc:{celexNumber}_{lang}";
        await db.StringSetAsync(key, url, TimeSpan.FromDays(7));

        return url;
    }
}
