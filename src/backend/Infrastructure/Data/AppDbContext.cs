using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<LawDocument> LawDocuments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LawDocument>()
                .HasIndex(ld => ld.Id);

            modelBuilder.Entity<LawDocument>()
                .HasIndex(ld => ld.Celex)
                .IsUnique(true);
        } 
}
