using Microsoft.EntityFrameworkCore;
using ServicePractice.Models;

namespace ServicePractice.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

   
    public DbSet<SentMatches> SentMatches { get; set; }
    public DbSet<TeamStanding> TeamStandings { get; set; }
    

   
}
