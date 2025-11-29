using EntityFrameworkCoreNewFeatures.Data;
using EntityFrameworkCoreNewFeatures.Interceptors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
