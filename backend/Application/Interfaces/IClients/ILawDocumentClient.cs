using Domain.Entities;

namespace Application.Interfaces.IClients;

public interface ILawDocumentClient
{
    Task<List<LawDocument>> GetLawsAsync(int limit, int offset);

    Task<Stream?> DownloadPdfAsync(string celex, string lang);
}
