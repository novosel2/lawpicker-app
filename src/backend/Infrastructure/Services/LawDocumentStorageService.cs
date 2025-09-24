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
    private readonly BlobContainerClient _blobContainer;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LawDocumentStorageService> _logger;
    private const string CONTAINER_NAME = "lawscontainer";

    public LawDocumentStorageService(IConfiguration configuration, ILogger<LawDocumentStorageService> logger)
    {
        _logger = logger;

        var azureConnectionString = configuration.GetConnectionString("AzureStorage");

        var blobServiceClient = new BlobServiceClient(azureConnectionString);
        _blobContainer = blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
        _blobContainer.CreateIfNotExists();

        var redisConnectionString = configuration.GetConnectionString("Redis");

        _redis = ConnectionMultiplexer.Connect(redisConnectionString!);
    }


    public async Task<bool> ExistsInCacheAsync(string celexNumber, string lang)
    {
        var db = _redis.GetDatabase();
        var blobClient = _blobContainer.GetBlobClient($"{celexNumber}@{lang}.pdf");

        string key = $"doc:{celexNumber}@{lang}";

        bool exists = await db.KeyExistsAsync(key);
        
        if (!exists && !await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Document {Celex} not found in the blob storage", celexNumber);
            return false;
        }


        var uri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7));
        await db.StringSetAsync(key, uri.ToString(), TimeSpan.FromDays(7));

        return true;
    }


    public async Task<string?> GetFromStorageAsync(string celexNumber, string lang)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            var blobClient = _blobContainer.GetBlobClient($"{celexNumber}@{lang}.pdf");
            var db = _redis.GetDatabase();

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Document {Celex} not found in the blob storage", celexNumber);
                return null;
            }

            string key = $"doc:{celexNumber}@{lang}";
            string? url = await db.StringGetAsync(key);

            if (string.IsNullOrEmpty(url) || await db.KeyExpireTimeAsync(key) <= DateTime.UtcNow.AddDays(1))
            {
                var uri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7));
                await db.StringSetAsync(key, uri.ToString(), TimeSpan.FromDays(7));
            }

            _logger.LogDebug("{Celex} pulled from cache in {Estimated}ms", celexNumber, sw.ElapsedMilliseconds);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {Celex} from blob storage", celexNumber);
            return null;
        }
    }


    public async Task<string?> StoreDocumentAsync(string celexNumber, string lang, Stream content)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            var blobClient = _blobContainer.GetBlobClient($"{celexNumber}@{lang}.pdf");
            content.Position = 0;
            await blobClient.UploadAsync(content, overwrite: true);

            Uri url = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(7));
            string key = $"doc:{celexNumber}@{lang}";

            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, url.ToString(), TimeSpan.FromDays(7));

            _logger.LogInformation("Document cached: {Celex} took {Estimated}ms", celexNumber, sw.ElapsedMilliseconds);
            return url.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document {Celex}", celexNumber);
            return null;
        }
    }


    public async Task<Dictionary<string, bool>> CheckBulkExistenceAsync(List<string> celexNumbers, string lang)
    {
        var db = _redis.GetDatabase();
        var results = new Dictionary<string, bool>();
        
        foreach (var celex in celexNumbers)
        {
            results[celex] = await db.KeyExistsAsync($"doc:{celex}@{lang}");
        }
        
        return results;
    }
}
