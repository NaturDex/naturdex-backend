using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NaturDex.Core.Data;
using NaturDex.Core.Interfaces;
using NaturDex.Core.Models;
using NaturDex.Core.Repositories;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (connectionString != null &&
    (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432;

    var builderNpgsql = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Username = userInfo[0],
        Password = userInfo[1],
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Require,
    };

    connectionString = builderNpgsql.ToString();
}

builder.Services.AddDbContext<NaturDexDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IRepository<Animal>, AnimalRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "NaturDex API",
        Description = "API til at tilg� NaturDex-databasen. Alle endpoints bruger JSON.",
        Contact = new OpenApiContact
        {
            Name = "NaturDex Team",
            Email = "kontakt@natudex.dk"
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();

app.UseCors("FrontendCorsPolicy");
app.UseHttpMetrics();
app.MapMetrics();

app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NaturDex API v1");
        c.RoutePrefix = "";
        c.DocumentTitle = "NaturDex API Dokumentation";
    });


app.UseHttpsRedirection();

app.MapGet("/api/v1/animals/{id}", async (int id, IRepository<Animal> repo) =>
{
    if (id <= 0) return Results.BadRequest("Invalid animal ID");
    var animal = await repo.GetByIdAsync(id);
    return animal is null ? Results.NotFound($"Animal with ID {id} not found") : Results.Ok(animal);
});

app.MapGet("/api/v1/animals", async (IRepository<Animal> repo) =>
{
    var animals = await repo.GetAllAsync();
    return animals is null || animals.Count == 0 ? Results.NotFound("No animals found") : Results.Ok(animals);
});

app.MapPost("/api/v1/animals", async (Animal animal, IRepository<Animal> repo) =>
{
    if (animal is null) return Results.BadRequest("Animal is required");
    if (string.IsNullOrWhiteSpace(animal.Species)) return Results.BadRequest("Animal name is required");
    var createdAnimal = await repo.CreateAsync(animal);
    return Results.Created($"/api/v1/animals/{createdAnimal.Id}", createdAnimal);
});

app.MapPut("/api/v1/animals/{id}", async (int id, Animal animal, IRepository<Animal> repo) =>
{
    if (animal is null) return Results.BadRequest("Animal is required");
    animal.Id = id;
    var updatedAnimal = await repo.UpdateAsync(animal);
    return Results.Ok(updatedAnimal);
});

app.MapDelete("/api/v1/animals/{id}", async (int id, IRepository<Animal> repo) =>
{
    if (id <= 0) return Results.BadRequest("Id must be over 0");
    await repo.DeleteAsync(id);
    return Results.NoContent();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NaturDexDbContext>();
    db.Database.Migrate();
}

app.Run();