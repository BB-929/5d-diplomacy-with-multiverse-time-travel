using Context;
using Factories;
using Mappers;
using Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.WithOrigins(
            "https://5d-diplomacy-with-multiverse-time-t.vercel.app",
            "http://localhost:5173",
            "http://localhost:3000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var provider = builder.Configuration["Provider"];
switch (provider)
{
    case "Sqlite":
        {
            builder.Services.AddDbContext<GameContext, SqliteGameContext>();
            break;
        }
    case "SqlServer":
        {
            builder.Services.AddDbContext<GameContext, SqlServerGameContext>();
            break;
        }
    default:
        {
            throw new ArgumentException($"Invalid provider: {provider}");
        }
}

builder.Services.AddScoped<EntityMapper>();
builder.Services.AddScoped<ModelMapper>();
builder.Services.AddScoped<GameRepository>();
builder.Services.AddScoped<WorldRepository>();
builder.Services.AddSingleton<RegionMapFactory>();
builder.Services.AddSingleton<DefaultWorldFactory>();

var app = builder.Build();

// CRITICAL: Middleware order matters!
app.UseCors("AllowVercel");  // CORS must come FIRST
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
