using BE_SaleHunter.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BE_SaleHunter.Infrastructure.Data
{
    public class SaleHunterDbContext : DbContext
    {
        public SaleHunterDbContext(DbContextOptions<SaleHunterDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductPrice> ProductPrices { get; set; }
        public DbSet<ProductRating> ProductRatings { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<ProductView> ProductViews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure User entity
            builder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.ProfileImageUrl).HasMaxLength(500);

                // One-to-One relationship with Store
                entity.HasOne(u => u.Store)
                    .WithOne(s => s.User)
                    .HasForeignKey<Store>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Store entity
            builder.Entity<Store>(entity =>
            {
                entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Type).IsRequired().HasMaxLength(20).HasDefaultValue("local");
                entity.Property(s => s.LogoUrl).HasMaxLength(500);
                entity.Property(s => s.Phone).HasMaxLength(20);
                entity.Property(s => s.Address).HasMaxLength(500);
                entity.Property(s => s.Category).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Description).HasMaxLength(1000);
                entity.Property(s => s.Latitude).HasColumnType("decimal(10,8)");
                entity.Property(s => s.Longitude).HasColumnType("decimal(11,8)");
                entity.Property(s => s.WhatsappPhone).HasMaxLength(20);
                entity.Property(s => s.FacebookUrl).HasMaxLength(200);
                entity.Property(s => s.InstagramUrl).HasMaxLength(200);
                entity.Property(s => s.WebsiteUrl).HasMaxLength(200);

                entity.HasIndex(s => s.UserId).IsUnique();
            });

            // Configure Product entity
            builder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.NameArabic).HasMaxLength(200);
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
                entity.Property(p => p.SalePercent).HasDefaultValue(0);
                entity.Property(p => p.Brand).HasMaxLength(100);
                entity.Property(p => p.Category).IsRequired().HasMaxLength(100);
                entity.Property(p => p.CategoryArabic).HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(2000);
                entity.Property(p => p.DescriptionArabic).HasMaxLength(2000);
                entity.Property(p => p.SourceUrl).HasMaxLength(500);

                // One-to-Many relationship with Store
                entity.HasOne(p => p.Store)
                    .WithMany(s => s.Products)
                    .HasForeignKey(p => p.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.Brand);
                entity.HasIndex(p => p.Price);
            });

            // Configure ProductImage entity
            builder.Entity<ProductImage>(entity =>
            {
                entity.Property(pi => pi.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(pi => pi.DisplayOrder).HasDefaultValue(0);

                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductPrice entity
            builder.Entity<ProductPrice>(entity =>
            {
                entity.Property(pp => pp.Price).HasColumnType("decimal(10,2)");

                entity.HasOne(pp => pp.Product)
                    .WithMany(p => p.PriceHistory)
                    .HasForeignKey(pp => pp.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductRating entity
            builder.Entity<ProductRating>(entity =>
            {
                entity.Property(pr => pr.Rating).IsRequired();
                entity.Property(pr => pr.Comment).HasMaxLength(500);

                entity.HasOne(pr => pr.Product)
                    .WithMany(p => p.Ratings)
                    .HasForeignKey(pr => pr.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pr => pr.User)
                    .WithMany(u => u.ProductRatings)
                    .HasForeignKey(pr => pr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint: one rating per user per product
                entity.HasIndex(pr => new { pr.UserId, pr.ProductId }).IsUnique();
            });

            // Configure UserFavorite entity
            builder.Entity<UserFavorite>(entity =>
            {
                entity.HasOne(uf => uf.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(uf => uf.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(uf => uf.Product)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(uf => uf.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: one favorite per user per product
                entity.HasIndex(uf => new { uf.UserId, uf.ProductId }).IsUnique();
            });

            // Configure ProductView entity
            builder.Entity<ProductView>(entity =>
            {
                entity.Property(pv => pv.ViewedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(pv => pv.User)
                    .WithMany(u => u.ProductViews)
                    .HasForeignKey(pv => pv.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pv => pv.Product)
                    .WithMany(p => p.Views)
                    .HasForeignKey(pv => pv.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(pv => pv.ViewedAt);
            });

            // Configure base entity properties for all entities
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}