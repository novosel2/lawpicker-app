using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Language
{
    [Key]
    [Required]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string LanguageName { get; set; } = null!;

    public ICollection<DocumentLanguage> DocumentLanguages { get; set; } = [];
}
