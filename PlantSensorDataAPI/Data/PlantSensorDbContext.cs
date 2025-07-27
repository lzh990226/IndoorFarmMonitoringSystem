using Microsoft.EntityFrameworkCore;
using PlantSensorDataAPI.Models;

namespace PlantSensorDataAPI.Data;

public class PlantSensorDbContext : DbContext
{
    public PlantSensorDbContext(DbContextOptions<PlantSensorDbContext> options) : base(options)
    {
    }

    public DbSet<CombinedPlantData> CombinedPlantData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CombinedPlantData>(entity =>
        {
            entity.HasKey(e => e.TrayId);
            entity.Property(e => e.PlantType).HasMaxLength(100);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        base.OnModelCreating(modelBuilder);
    }
}
