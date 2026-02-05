using Context;
using Factories;
using Mappers;
using Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

// 1. Add this in the "builder" section (before var app = builder.Build();)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseRouting():
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();
app.Run();




