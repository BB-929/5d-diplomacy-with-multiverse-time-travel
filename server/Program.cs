using Context;
using Factories;
using Mappers;
using Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS Configuration - FIXED!
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.WithOrigins(
            "https://5d-diplomacy-with-multiverse-time-t.vercel.app",  // Your VERCEL URL
            "http://localhost:5173",                                    // Local development
            "http://localhost:3000"                                     // Alternative local port
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

app.UseRouting();
app.UseCors("AllowVercel");  // Apply CORS policy
app.UseAuthorization();
app.MapControllers();

app.Run();
