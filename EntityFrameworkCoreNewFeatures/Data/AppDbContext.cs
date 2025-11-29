using EntityFrameworkCoreNewFeatures.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCoreNewFeatures.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
                
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e=>e.Id);
                entity.Property(e => e.Name)
                .IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description)
                .HasMaxLength(500);
                //Make Category table Temporal, this way history table creates automatically
                entity.ToTable("Categories", b => b.IsTemporal(temporal =>
                {
                    temporal.HasPeriodStart("PeriodStart");
                    temporal.HasPeriodEnd("PeriodEnd");
                    temporal.UseHistoryTable("CategoriesHistory");
                }));
                //Global query filter filter soft delete records
                entity.HasQueryFilter(c => !c.IsDeleted);
                //initial seed data for categories table 
                entity.HasData(
                    new Category { Id=1, Name="Electronics", Description="Electronic Devices", CreatedDate=DateTime.Now, ModifiedDate=DateTime.Now},
                    new Category { Id=2, Name="Books", Description="Story Books", CreatedDate=DateTime.Now, ModifiedDate=DateTime.Now},
                    new Category { Id=3, Name="Clothing", Description="Winter Clothes", CreatedDate=DateTime.Now, ModifiedDate=DateTime.Now}
                    );
            });
            //Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                .IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)").IsRequired();
                //Json column Sepecifications EF Core 8 Feature
                entity.OwnsOne(e => e.objSpecifications, ownedNavigationBuilder =>
                {
                    ownedNavigationBuilder.ToJson();
                });
                //Foriegn Key Relationship
                entity.HasOne(p=> p.Category)
                    .WithMany(p=>p.Products)
                    .HasForeignKey(p=>p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                //Enable Temporal Table
                entity.ToTable("Products", b => b.IsTemporal(temporal =>
                {
                    temporal.HasPeriodStart("PeriodStart");
                    temporal.HasPeriodEnd("PeriodEnd");
                    temporal.UseHistoryTable("ProductsHistory");
                }));
                //Global query filter, this automatically exclude soft delete products
                entity.HasQueryFilter(p => !p.IsDeleted);

                //Create index for performance
                entity.HasIndex(p => p.CategoryId);
                entity.HasIndex(p => p.IsDeleted);


            });

        }

        public override int SaveChanges()
        {
            UpdateTimeStamp();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimeStamp();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimeStamp()
        {
            var entries= ChangeTracker.Entries()
                    .Where(e=> e.State == EntityState.Added || e.State == EntityState.Modified);
            foreach (var entry in entries)
            {
                if(entry.Entity is Product product)
                {
                    if(entry.State == EntityState.Added)
                    {
                        product.CreatedDate = DateTime.Now;
                    }
                    else if(entry.State == EntityState.Modified)
                    {
                        product.ModifiedDate = DateTime.Now;
                    }
                }
                else if(entry.Entity is Category category) 
                {
                    if (entry.State == EntityState.Added)
                    {
                        category.CreatedDate = DateTime.Now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        category.ModifiedDate = DateTime.Now;
                    }
                }
            }
        }

        public async Task SeedProductIntialDataAsync()
        {
            if(await Products.AnyAsync())
            {
                return;//Data already seeded
            }
            var products = new[]
            {
                new Product
                {
                    Name="Laptop",
                    Description="High performance Laptop",
                    Price=1220m,
                    CategoryId=1,
                    objSpecifications=new ProductSpecifications
                    {
                        Color="Red",
                        Brand="MSI",
                        Model="Pro",
                        Weight=1.5,
                        WarrantyMonths=12,
                        InStock=true
                    }
                },
                new Product
                {
                    Name="SmartPhone",
                    Description="Latest Smartphone",
                    Price=720m,
                    CategoryId=1,
                    objSpecifications=new ProductSpecifications
                    {
                        Color="Black",
                        Brand="Tech",
                        Model="X11",
                        Weight=0.5,
                        WarrantyMonths=12,
                        InStock=true
                    }
                }
            };
            await Products.AddRangeAsync(products);
            await SaveChangesAsync();
        }




    }
}
