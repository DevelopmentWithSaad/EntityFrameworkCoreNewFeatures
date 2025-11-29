using EntityFrameworkCoreNewFeatures.Data;
using EntityFrameworkCoreNewFeatures.DTOs;
using EntityFrameworkCoreNewFeatures.Interceptors;
using EntityFrameworkCoreNewFeatures.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Configure Json to handle circular refrences
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
//Register Dbcontext with sql server and interceptor
builder.Services.AddDbContext<AppDbContext>((serviceprovider, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqloptions =>
        {
            //Enable Temporal Table support
            sqloptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
         //Add custom interceptop of loggin and auditing
        .AddInterceptors(new CustomSaveChangesInterceptor());
});


var app = builder.Build();

//Seed Database with initial Data
using(var scope = app.Services.CreateScope())
{
    var context=scope.ServiceProvider.GetService<AppDbContext>();
    //Ensure Database is ready and apply pending migrations
    await context.Database.MigrateAsync();
    //Seed initial data only run if database is empty
    await context.SeedProductIntialDataAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

//1: AsNoTracking
app.MapGet("api/categories",async(AppDbContext db) =>
{
    var categories =await db.Categories
        .AsNoTracking()//No Change Tracking
        .ToListAsync();
    return Results.Ok(categories);
})
    .WithName("GetCategories")
    .WithOpenApi();
//.RequireAuthorization() for [Authorize] JWT 
//1.1 AsNoTracking with Navigation properties
app.MapGet("api/categories/{id}", async (int id, AppDbContext db) =>
{
    var category =await db.Categories
    .AsNoTracking()
    .Include(c => c.Products)
    .FirstOrDefaultAsync(c => c.Id == id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
})
    .WithName("GetCategoryById")
    .WithOpenApi();
//2 GetProducts with Category and Json columns
app.MapGet("api/getproducts", async (AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking().Include(p => p.Category).ToListAsync();
    return Results.Ok(products);
})
    .WithName("GetProducts")
    .WithOpenApi();
//2.2 Get Products with Category and Json columns by specify column names
app.MapGet("api/getproductsn", async (AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking()
    .Select(p => new
    {
        p.Id,
        p.Name,
        p.Price,
        p.Description,
        p.CategoryId,
        CategoryName=p.Category.Name,
        p.objSpecifications,
        p.CreatedDate,
        p.ModifiedDate
    })        
    .ToListAsync();
    return Results.Ok(products);
})
    .WithName("GetProductsn")
    .WithOpenApi();
//3. Query with json column properties
app.MapGet("api/products/by-color/{color}", async(string color,AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking()
    .Where(p => p.objSpecifications.Color.ToLower() == color.ToLower())
    .Select(p => new
    {
        p.Id,
        p.Price,
        p.Name,
        p.Description,
        p.CategoryId,
        p.objSpecifications
    }).ToListAsync();
    return Results.Ok(products);
})
    .WithName("GetProductByColor")
    .WithOpenApi();
//4:Split Query example to avoid cartesion explusions for include entities
app.MapGet("api/categories/withproductsplit", async(AppDbContext db) =>
{
    var categories=await db.Categories.AsNoTracking().AsSplitQuery().Include(p=>p.Products).ToListAsync();
    return Results.Ok(categories);
})
    .WithName("GetCategorieswithsplit")
    .WithOpenApi();
//5:Create Product
app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    // Standard insert with tracking
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", new
    {
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.CategoryId,
        product.objSpecifications,
        product.CreatedDate,
        product.ModifiedDate
    });
})
.WithName("CreateProduct")
.WithOpenApi();
//6: get single product by id
app.MapGet("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products
        .AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.CategoryId,
            CategoryName = p.Category.Name,
            p.objSpecifications,
            p.CreatedDate,
            p.ModifiedDate
        })
        .FirstOrDefaultAsync();

    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithOpenApi();
//7: Bulk update
app.MapPut("/api/products/bulk-update-price", async (decimal percentage, AppDbContext db) =>
{
    // ExecuteUpdate - Bulk update without loading entities into memory
    // This is much faster than loading, updating, and saving
    var affectedRows = await db.Products
        .Where(p => p.Price > 0)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.Price, p => p.Price * (1 + percentage / 100))
            .SetProperty(p => p.ModifiedDate, DateTime.Now));

    return Results.Ok(new { Message = $"Updated {affectedRows} products" });
})
.WithName("BulkUpdateProductPrice")
.WithOpenApi();
//8:Bulk Delete
app.MapDelete("/api/products/bulk-delete", async (decimal maxPrice, AppDbContext db) =>
{
    // ExecuteDelete - Bulk delete without loading entities
    // Soft delete using IsDeleted flag (respects Global Query Filter)
    var affectedRows = await db.Products
        .Where(p => p.Price <= maxPrice)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.IsDeleted, true)
            .SetProperty(p => p.ModifiedDate, DateTime.UtcNow));

    return Results.Ok(new { Message = $"Soft deleted {affectedRows} products" });
})
.WithName("BulkDeleteProducts")
.WithOpenApi();
//9: hard delete bypass global query filter
app.MapDelete("/api/products/{id}/hard-delete", async (int id, AppDbContext db) =>
{
    // IgnoreQueryFilters to bypass the IsDeleted filter
    var product = await db.Products
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product is null)
        return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("HardDeleteProduct")
.WithOpenApi();
//10:Temporal table query
app.MapGet("/api/products/{id}/history", async (int id, AppDbContext db) =>
{
    // Temporal tables allow querying historical data
    var history = await db.Products
        .TemporalAll() // Gets all historical versions
        .Where(p => p.Id == id)
        .OrderBy(p => EF.Property<DateTime>(p, "PeriodStart"))
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            PeriodStart = EF.Property<DateTime>(p, "PeriodStart"),
            PeriodEnd = EF.Property<DateTime>(p, "PeriodEnd")
        })
        .ToListAsync();

    return Results.Ok(history);
})
.WithName("GetProductHistory")
.WithOpenApi();
//11:Temporal table as of date
app.MapGet("/api/products/{id}/as-of/{date}", async (int id, DateTime date, AppDbContext db) =>
{
    // Query temporal table as of a specific date
    var product = await db.Products
        .TemporalAsOf(date)
        .FirstOrDefaultAsync(p => p.Id == id);

    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductAsOf")
.WithOpenApi();
//12:Product insert using storeprocedure
app.MapPost("/api/products/sp-insert", async (ProductDto dto, AppDbContext db) =>
{
    // Call stored procedure for insert
    var specsJson = dto.ProductSpecifications != null
        ? JsonSerializer.Serialize(dto.ProductSpecifications)
        : "{}";

    await db.Database.ExecuteSqlRawAsync(
        "EXEC sp_InsertProduct @p0, @p1, @p2, @p3",
        dto.Name, dto.Price, dto.CategoryId, specsJson);

    return Results.Ok(new { Message = "Product inserted via stored procedure" });
})
.WithName("InsertProductSP")
.WithOpenApi();
//13: stored procedure update
app.MapPut("/api/products/{id}/sp-update", async (int id, ProductDto dto, AppDbContext db) =>
{
    // Call stored procedure for update
    var result = await db.Database.ExecuteSqlRawAsync(
        "EXEC sp_UpdateProduct @p0, @p1, @p2",
        id, dto.Name, dto.Price);

    return result > 0
        ? Results.Ok(new { Message = "Product updated via stored procedure" })
        : Results.NotFound();
})
.WithName("UpdateProductSP")
.WithOpenApi();
//14:stored procedure delete
app.MapDelete("/api/products/{id}/sp-delete", async (int id, AppDbContext db) =>
{
    // Call stored procedure for delete (soft delete)
    var result = await db.Database.ExecuteSqlRawAsync(
        "EXEC sp_DeleteProduct @p0", id);

    return result > 0
        ? Results.Ok(new { Message = "Product deleted via stored procedure" })
        : Results.NotFound();
})
.WithName("DeleteProductSP")
.WithOpenApi();
//15:get deleted products ignore query filter
app.MapGet("/api/products/deleted", async (AppDbContext db) =>
{
    // IgnoreQueryFilters to see deleted products
    var deletedProducts = await db.Products
        .IgnoreQueryFilters()
        .Where(p => p.IsDeleted)
        .AsNoTracking()
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.CategoryId,
            CategoryName = p.Category.Name,
            p.objSpecifications,
            p.CreatedDate,
            p.ModifiedDate,
            p.IsDeleted
        })
        .ToListAsync();

    return Results.Ok(deletedProducts);
})
.WithName("GetDeletedProducts")
.WithOpenApi();
//16: restore deleted product as deleted=false
app.MapPut("/api/products/{id}/restore", async (int id, AppDbContext db) =>
{
    var product = await db.Products
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);

    if (product is null)
        return Results.NotFound();

    product.IsDeleted = false;
    product.ModifiedDate = DateTime.UtcNow;
    await db.SaveChangesAsync();

    // Return without navigation properties to avoid circular reference
    return Results.Ok(new
    {
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.CategoryId,
        product.objSpecifications,
        product.CreatedDate,
        product.ModifiedDate,
        product.IsDeleted
    });
})
.WithName("RestoreProduct")
.WithOpenApi();


app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
