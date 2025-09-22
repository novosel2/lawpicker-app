namespace Application.Interfaces.IServices;

public interface ILawDocumentStorageService
{
    /// <summary>
    /// Checks if file exists in the cache
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <param name="lang">Language of the document</param>
    /// <returns>True if exists, false if not</returns>
    Task<bool> ExistsInCacheAsync(string celexNumber, string lang);

    /// <summary>
    /// Gets a file from storage
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <param name="lang">Language of the document</param>
    /// <returns>Cached document url</returns>
    Task<string?> GetFromStorageAsync(string celexNumber, string lang);

    /// <summary>
    /// Stores a file in the storage
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <param name="lang">Language of the document</param>
    /// <param name="content">File stream of the document</param>
    /// <returns>Url of the stored document</returns>
    Task<string?> StoreDocumentAsync(string celexNumber, string lang, Stream content);

    /// <summary>
    /// Checks for all celex in list if they exist in cache
    /// </summary>
    /// <param name="celexNumbers">List of celex numbers</param>
    /// <param name="lang">Language of documents</param>
    /// <returns>Dictionary of celex numbers and if they are cached or not</returns>
    Task<Dictionary<string, bool>> CheckBulkExistenceAsync(List<string> celexNumbers, string lang);
}
