using Microsoft.EntityFrameworkCore;

namespace MovieBox.Models;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<List> Lists { get; set; }
    public DbSet<Movie?> Movies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategorizedItems> CategorizedItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CategorizedItems>()
            .HasOne(ci => ci.Category)
            .WithMany(c => c.CategorizedItems)
            .HasForeignKey(ci => ci.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CategorizedItems>()
            .HasOne(ci => ci.Movie)
            .WithMany()
            .HasForeignKey(ci => ci.MovieId)
            .OnDelete(DeleteBehavior.NoAction);
    }

}