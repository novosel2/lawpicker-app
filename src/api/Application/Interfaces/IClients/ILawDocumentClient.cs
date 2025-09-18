using Domain.Entities;

namespace Application.Interfaces.IClients;

public interface ILawDocumentClient
{
    Task<List<LawDocument>> GetLawsAsync(int limit, int offset);

    Task<byte[]> DownloadPdfAsync(string celex, string lang);
}
