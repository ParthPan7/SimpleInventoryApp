using Microsoft.EntityFrameworkCore;

namespace SimpleInventoryApp
{
    public class ProductDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public ProductDbContext()
        {

        }

        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(optionsBuilder.IsConfigured == true) { return; }
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=ProductInventoryDB;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasIndex(i => new { i.ProductName })
                .IsUnique();

            modelBuilder.Entity<Product>()
              .Property(p => p.ProductName)
              .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.ProductCategory)
                .IsRequired();

        }

    }
}
