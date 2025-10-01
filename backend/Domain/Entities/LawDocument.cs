using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class LawDocument
{
    [Key]
    [Required]
    public string Celex { get; set; } = null!;

    [Required]
    [RegularExpression("^[RLD]$", ErrorMessage = "Type must be R, L, or D.")]
    public char Type { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public ICollection<DocumentLanguage> DocumentLanguages { get; set; } = [];
}
