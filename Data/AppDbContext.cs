using Microsoft.EntityFrameworkCore;
using ServicePractice.Models;

namespace ServicePractice.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<SentMatches> SentMatches { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>().HasData(
        new Product
        {
            Id = 1,
            Name = "Mouse",
            Price = 500,
            IsActive = true
        },
        new Product
        {
            Id = 2,
            Name = "Keyboard",
            Price = 1500,
            IsActive = true
        },
        new Product
        {
            Id = 3,
            Name = "Old Monitor",
            Price = 3000,
            IsActive = false
        }
    );
}
}