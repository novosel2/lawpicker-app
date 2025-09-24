using Domain.Entities;

namespace Application.Interfaces.IRepositories;

public interface ILawDocumentRepository
{
    /// <summary>
    /// Adds a chunk of law documents to the database
    /// </summary>
    /// <param name="lawDocuments">List of law documents to be added</param>
    Task AddChunkAsync(List<LawDocument> lawDocuments);

    /// <summary>
    /// Get law documents for the selected page, optionally filtered by type or search
    /// </summary>
    /// <param name="documentTypes">Document type</param>
    /// <param name="search">Search query for law document names</param>
    /// <param name="lang">Language of the documents</param>
    /// <param name="page">Selected page number</param>
    /// <param name="limit">Limit of returned law documents</param>
    /// <returns>List of law documents</returns>
    Task<List<LawDocument>> GetLawDocumentsAsync(string? documentTypes, string? search, string lang, int page, int limit);

    /// <summary>
    /// Gets total number of documents with specified filters
    /// </summary>
    /// <param name="documentTypes">Document type</param>
    /// <param name="search">Search query for law document names</param>
    /// <param name="lang">Language of the documents</param>
    /// <returns>Total number of documents with filters</returns>
    Task<int> GetLawDocumentsCountAsync(string? documentTypes, string? search, string lang);

    /// <summary>
    /// Get law document by celex
    /// </summary>
    /// <param name="celex">Celex of the law document</param>
    /// <returns>A law document with specified celex</returns>
    Task<LawDocument?> GetLawDocumentByCelexAsync(string celex);

    /// <summary>
    /// Get all EU languages
    /// </summary>
    /// <returns>List of all EU languages</returns>
    Task<List<Language>> GetAllLanguagesAsync();

    /// <summary>
    /// Checks if any changes have been made
    /// </summary>
    /// <returns>True if saved, false if not</returns>
    Task<bool> IsSavedAsync();
}
