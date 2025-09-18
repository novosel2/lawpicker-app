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
    /// <param name="page">Selected page number</param>
    /// <param name="limit">Limit of returned law documents</param>
    /// <returns>List of law documents</returns>
    Task<List<LawDocument>> GetLawDocumentsAsync(string? documentTypes, string? search, int page, int limit);

    /// <summary>
    /// Gets total number of documents with specified filters
    /// </summary>
    /// <param name="documentTypes">Document type</param>
    /// <param name="search">Search query for law document names</param>
    /// <returns>Total number of documents with filters</returns>
    Task<int> GetLawDocumentsCountAsync(string? documentTypes, string? search);

    /// <summary>
    /// Get law document by celex
    /// </summary>
    /// <param name="celex">Celex of the law document</param>
    /// <returns>A law document with specified celex</returns>
    Task<LawDocument?> GetLawDocumentByCelexAsync(string celex);

    /// <summary>
    /// Checks if any changes have been made
    /// </summary>
    /// <returns>True if saved, false if not</returns>
    Task<bool> IsSavedAsync();
}
