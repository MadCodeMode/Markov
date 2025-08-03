using Markov.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace Markov.Services
{
    public class MarkovDbContext : DbContext
    {
        public MarkovDbContext(DbContextOptions<MarkovDbContext> options) : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Candle> Candles { get; set; }
        public DbSet<StrategyConfiguration> StrategyConfigurations { get; set; }
        public DbSet<LiveSession> LiveSessions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>()
                        .HasMany(a => a.HistoricalData)
                        .WithOne()
                        .HasForeignKey("AssetId");
        }
    }
}
