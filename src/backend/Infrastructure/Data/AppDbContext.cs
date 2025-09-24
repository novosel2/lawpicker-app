using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<LawDocument> LawDocuments { get; set; } = null!;
    public DbSet<Language> Languages { get; set; } = null!;
    public DbSet<DocumentLanguage> DocumentLanguages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LawDocument>(entity => 
        {
            entity.HasKey(ld => ld.Celex);
            entity.HasIndex(ld => ld.Celex).IsUnique();
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(l => l.LanguageCode);
            entity.Property(l => l.LanguageCode).HasMaxLength(3);
            entity.Property(l => l.LanguageName).HasMaxLength(30);
        });

        modelBuilder.Entity<DocumentLanguage>(entity =>
        {
            entity.HasKey(dl => new { dl.CelexNumber, dl.LanguageCode });
            
            entity.HasOne(dl => dl.LawDocument)
                .WithMany(ld => ld.DocumentLanguages)
                .HasForeignKey(dl => dl.CelexNumber)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(dl => dl.Language)
                .WithMany(l => l.DocumentLanguages)
                .HasForeignKey(dl => dl.LanguageCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedLanguages(modelBuilder);
    } 

    private static void SeedLanguages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>().HasData(
            // All 24 official EU languages (as of 2024)
            new Language { LanguageCode = "BG", LanguageName = "Bulgarian" },
            new Language { LanguageCode = "HR", LanguageName = "Croatian" },
            new Language { LanguageCode = "CS", LanguageName = "Czech" },
            new Language { LanguageCode = "DA", LanguageName = "Danish" },
            new Language { LanguageCode = "NL", LanguageName = "Dutch" },
            new Language { LanguageCode = "EN", LanguageName = "English" },
            new Language { LanguageCode = "ET", LanguageName = "Estonian" },
            new Language { LanguageCode = "FI", LanguageName = "Finnish" },
            new Language { LanguageCode = "FR", LanguageName = "French" },
            new Language { LanguageCode = "DE", LanguageName = "German" },
            new Language { LanguageCode = "EL", LanguageName = "Greek" },
            new Language { LanguageCode = "HU", LanguageName = "Hungarian" },
            new Language { LanguageCode = "GA", LanguageName = "Irish" },
            new Language { LanguageCode = "IT", LanguageName = "Italian" },
            new Language { LanguageCode = "LV", LanguageName = "Latvian" },
            new Language { LanguageCode = "LT", LanguageName = "Lithuanian" },
            new Language { LanguageCode = "MT", LanguageName = "Maltese" },
            new Language { LanguageCode = "PL", LanguageName = "Polish" },
            new Language { LanguageCode = "PT", LanguageName = "Portuguese" },
            new Language { LanguageCode = "RO", LanguageName = "Romanian" },
            new Language { LanguageCode = "SK", LanguageName = "Slovak" },
            new Language { LanguageCode = "SL", LanguageName = "Slovenian" },
            new Language { LanguageCode = "ES", LanguageName = "Spanish" },
            new Language { LanguageCode = "SV", LanguageName = "Swedish" } 
        );
    }
}
