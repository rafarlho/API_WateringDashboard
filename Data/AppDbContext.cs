using API_WateringDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace API_WateringDashboard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reading>(entity =>
        {
            entity.HasKey(e => new {e.Timestamp, e.Origin});
            entity.ToTable("readings"); 
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Origin).HasColumnName("origin");
            entity.Property(e => e.AirTemperature).HasColumnName("air_t");
            entity.Property(e => e.AirHumidity).HasColumnName("air_h");
            entity.Property(e => e.SoilHumidity).HasColumnName("soil_h");
            entity.Property(e => e.Status).HasColumnName("status");
        });
    }
}
