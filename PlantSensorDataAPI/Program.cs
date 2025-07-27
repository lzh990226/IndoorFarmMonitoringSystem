using Microsoft.EntityFrameworkCore;
using PlantSensorDataAPI.Configuration;
using PlantSensorDataAPI.Data;
using PlantSensorDataAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Configure external API settings
builder.Services.Configure<ExternalApiSettings>(
    builder.Configuration.GetSection(ExternalApiSettings.SectionName));

// Add Entity Framework and PostgreSQL
builder.Services.AddDbContext<PlantSensorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddScoped<IPlantDataService, PlantDataService>();

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Plant Sensor Data API",
        Version = "v1",
        Description = "API for monitoring indoor farm plant sensor data and configurations"
    });
});

var app = builder.Build();

// Ensure database is created (alternative to migrations)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PlantSensorDbContext>();
    context.Database.EnsureCreated(); // This will create the database and tables if they don't exist
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plant Sensor Data API v1");
        c.RoutePrefix = "doc"; // Swagger UI accessible at /doc
    });

    // Also keep OpenAPI endpoint
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
app.UseAuthorization();

app.MapControllers();

app.Run();
