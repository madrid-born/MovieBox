using Microsoft.EntityFrameworkCore;

namespace MovieBox.Models;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<List> Lists { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategorizedItems> CategorizedItems { get; set; }
}