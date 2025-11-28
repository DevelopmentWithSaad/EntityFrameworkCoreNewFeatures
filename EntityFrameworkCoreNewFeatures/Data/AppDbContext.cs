using EntityFrameworkCoreNewFeatures.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreNewFeatures.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
                
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> products { get; set; }

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


        }
    }
}
