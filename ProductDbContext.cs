using Microsoft.EntityFrameworkCore;

namespace SimpleInventoryApp
{
    public class ProductDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=ProductInventoryDB;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasIndex(i => new { i.ProductName })
                .IsUnique();
        }

    }
}
