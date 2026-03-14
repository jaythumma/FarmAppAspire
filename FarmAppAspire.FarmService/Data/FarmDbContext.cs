using FarmAppAspire.FarmService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FarmAppAspire.FarmService.Data;

public class FarmDbContext : DbContext
{
    public FarmDbContext(DbContextOptions<FarmDbContext> options) : base(options) { }

    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<CropSeason> CropSeasons => Set<CropSeason>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Farm>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasMany(f => f.Fields)
                .WithOne(x => x.Farm)
                .HasForeignKey(x => x.FarmId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Field>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Status).HasConversion<string>();
            e.Property(f => f.GpsPolygon)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<GpsPoint>>(v, (JsonSerializerOptions?)null) ?? new List<GpsPoint>())
                .HasColumnType("text");
            e.HasIndex(f => new { f.FarmId, f.Code }).IsUnique();
            e.HasMany(f => f.CropSeasons)
                .WithOne(cs => cs.Field)
                .HasForeignKey(cs => cs.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CropSeason>(e =>
        {
            e.HasKey(cs => cs.Id);
            e.Property(cs => cs.Status).HasConversion<string>();
        });
    }
}
