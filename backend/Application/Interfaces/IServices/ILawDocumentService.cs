using Application.Dto;

namespace Application.Interfaces.IServices;

public interface ILawDocumentService
{
    /// <summary>
    /// Fills the database with all the european laws
    /// </summary>
    Task ResetDatabaseAsync();

    /// <summary>
    /// Get law documents for the selected page, optionally filtered by type or search
    /// </summary>
    /// <param name="documentTypes">Document type</param>
    /// <param name="search">Search query for law document names</param>
    /// <param name="page">Selected page number</param>
    /// <param name="limit">Limit of returned law documents</param>
    /// <returns>List of law documents, with total number of documents with that filter</returns>
    Task<LawDocumentsListResponse> GetLawDocumentsAsync(string? documentTypes, string? search, int page, int limit);

    /// <summary>
    /// Gets selected law documents based on celex numbers
    /// </summary>
    /// <param name="celexNumbers">
    /// <param name="lang">Language code for the law document</param>
    /// <returns>A list of celex url responses</returns>
    Task<List<CelexUrlResponse>> GetLawDocumentFilesAsync(List<string> celexNumbers, string lang);

    /// <summary>
    /// Gets a url for the specified celex
    /// </summary>
    /// <param name="celex">Celex number of the law document</param>
    /// <param name="lang">Language code you want the law document in</param>
    /// <returns>A celex url response</returns>
    Task<CelexUrlResponse> GetUrlByCelexAsync(string celex, string lang);
}
