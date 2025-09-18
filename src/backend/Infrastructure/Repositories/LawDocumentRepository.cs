using Application.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LawDocumentRepository : ILawDocumentRepository
{
    private readonly AppDbContext _db;

    public LawDocumentRepository(AppDbContext db)
    {
        _db = db;
    }


    public async Task AddChunkAsync(List<LawDocument> lawDocuments)
    {
        await _db.LawDocuments.AddRangeAsync(lawDocuments.ToArray());
    }


    public async Task<List<LawDocument>> GetLawDocumentsAsync(string? documentTypes, string? search, int page, int limit) 
    {
        var query = _db.LawDocuments.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(documentTypes))
        {
            var typesArray = documentTypes.ToCharArray();
            query = query.Where(ld => typesArray.Contains(ld.Type));
        }

        if (search != null)
        {
            var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in searchWords)
                query = query.Where(ld => EF.Functions.Like(ld.Title.ToLower(), $"%{word.ToLower()}%"));
        }
        
        var lawDocuments = await query
            .OrderByDescending(ld => ld.Celex)
            .Skip(page * limit)
            .Take(limit)
            .ToListAsync();
            
        return lawDocuments;
    }
    
    
    public async Task<int> GetLawDocumentsCountAsync(string? documentTypes, string? search)
    {
        var query = _db.LawDocuments.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(documentTypes))
        {
            var typesArray = documentTypes.ToCharArray();
            query = query.Where(ld => typesArray.Contains(ld.Type));
        }

        if (search != null)
        {
            var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in searchWords)
                query = query.Where(ld => EF.Functions.Like(ld.Title.ToLower(), $"%{word.ToLower()}%"));
        }

        return await query.CountAsync();
    }


    public async Task<LawDocument?> GetLawDocumentByCelexAsync(string celex)
    {
        return await _db.LawDocuments.FirstOrDefaultAsync(ld => ld.Celex == celex);
    }


    public async Task<bool> IsSavedAsync()
    {
        int saved = await _db.SaveChangesAsync();

        return saved > 0;
    }
}
