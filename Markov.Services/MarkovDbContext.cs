using Markov.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace Markov.Services
{
    public class MarkovDbContext : DbContext
    {
        public MarkovDbContext(DbContextOptions<MarkovDbContext> options) : base(options)
        {
        }

        public virtual DbSet<Asset> Assets { get; set; }
        public virtual DbSet<Candle> Candles { get; set; }
        public virtual DbSet<StrategyConfiguration> StrategyConfigurations { get; set; }
        public virtual DbSet<LiveSession> LiveSessions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>()
                .HasMany(a => a.HistoricalData)
                .WithOne()
                .HasForeignKey("AssetId");
        }
    }
}