namespace Application.Dto;

public class DownloadResult
{
    public string Celex { get; set; } = null!;
    public Stream? Data { get; set; }
    public bool Success { get; set; }
}
