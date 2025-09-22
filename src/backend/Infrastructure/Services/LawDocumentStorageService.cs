using System.Diagnostics;
using Application.Interfaces.IServices;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class LawDocumentStorageService : ILawDocumentStorageService
{
    private readonly BlobContainerClient _blobContainer;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LawDocumentStorageService> _logger;
    private const string REDIS_SET = "lawdocs:cached";
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
        
        _logger.LogInformation("LawDocumentStorageService initialized successfully");
    }


    public async Task<bool> ExistsInCacheAsync(string celexNumber)
    {
        var db = _redis.GetDatabase();

        return await db.SetContainsAsync(REDIS_SET, celexNumber);
    }


    public async Task<Stream?> GetFromStorageAsync(string celexNumber)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("Pulling {Celex} from cache", celexNumber);

            var blobClient = _blobContainer.GetBlobClient($"{celexNumber}.pdf");

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Document {Celex} not found in the blob storage", celexNumber);
                return null;
            }

            var file = await blobClient.OpenReadAsync(new BlobOpenReadOptions(allowModifications: false)
            {
                BufferSize = 16 * 1024,
                Position = 0
            });

            _logger.LogDebug("{Celex} pulled from cache in {Estimated}", celexNumber, sw.ElapsedMilliseconds);
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {Celex} from blob storage", celexNumber);
            return null;
        }
    }


    public async Task StoreDocumentAsync(string celexNumber, Stream content)
    {
        try
        {
            var blobClient = _blobContainer.GetBlobClient($"{celexNumber}.pdf");
            content.Position = 0;
            await blobClient.UploadAsync(content, overwrite: true);

            var db = _redis.GetDatabase();
            await db.SetAddAsync(REDIS_SET, celexNumber);

            _logger.LogInformation("Cached document {Celex}", celexNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document {Celex}", celexNumber);
            throw;
        }
    }


    public async Task<Dictionary<string, bool>> CheckBulkExistenceAsync(List<string> celexNumbers)
    {
        var db = _redis.GetDatabase();
        var results = new Dictionary<string, bool>();
        
        foreach (var celex in celexNumbers)
        {
            results[celex] = await db.SetContainsAsync($"{REDIS_SET}", celex);
        }
        
        return results;
    }
}
