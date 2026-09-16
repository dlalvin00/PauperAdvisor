using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Domain.Entities;
using System.Text.Json;

namespace PauperAdvisor.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Card> Cards { get; set; }
    public DbSet<CardFace> CardFaces { get; set; }
    public DbSet<Ruling> Rulings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.OracleId);

            entity.Property(e => e.Colors).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.ColorIdentity).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.Keywords).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        });

        modelBuilder.Entity<Ruling>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.Card)
                .WithMany(p => p.Rulings)
                .HasForeignKey(d => d.CardOracleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardFace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.Card)
                .WithMany(p => p.CardFaces)
                .HasForeignKey(d => d.CardOracleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}