using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class DocumentLanguage
{
    [Required]
    public string CelexNumber { get; set; } = null!;
    public LawDocument? LawDocument { get; set; }

    [Required]
    public string LanguageCode { get; set; } = null!;
    public Language? Language { get; set; } 
}
