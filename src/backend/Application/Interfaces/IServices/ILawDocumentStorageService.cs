namespace Application.Interfaces.IServices;

public interface ILawDocumentStorageService
{
    /// <summary>
    /// Checks if file exists in the cache
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <returns>True if exists, false if not</returns>
    Task<bool> ExistsInCacheAsync(string celexNumber);

    /// <summary>
    /// Gets a file from storage
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <returns>File stream of requested document</returns>
    Task<Stream?> GetFromStorageAsync(string celexNumber);

    /// <summary>
    /// Stores a file in the storage
    /// </summary>
    /// <param name="celexNumber">Celex Number of the document</param>
    /// <param name="content">File stream of the document</param>
    Task StoreDocumentAsync(string celexNumber, Stream content);

    Task<Dictionary<string, bool>> CheckBulkExistenceAsync(List<string> celexNumbers);
}
