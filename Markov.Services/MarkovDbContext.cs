using Markov.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace Markov.Services
{
    public class MarkovDbContext : DbContext
    {
        public MarkovDbContext(DbContextOptions<MarkovDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
