using Domain.Entities;

namespace Application.Dto;

public class LawDocumentsListResponse
{
    public int Count { get; set; }
    public List<LawDocument> LawDocuments { get; set; } = null!;
}
