namespace Application.Dto;

public class CelexUrlResponse
{
    public string Celex { get; set; } = null!;
    public string? Url { get; set; }
    public string? Problem { get; set; }
    public List<string>? AvailableLanguages { get; set; } 
    public string RequestedLanguage { get; set; } = null!;
    public bool IsSuccess => !string.IsNullOrEmpty(Url) && string.IsNullOrEmpty(Problem);
}
