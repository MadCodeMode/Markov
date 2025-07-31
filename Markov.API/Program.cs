using HealthChecks.UI.Client;
using Markov.Services;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Repositories; // Assuming this namespace
using Markov.Services.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MarkovDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

// Add services to the container.
builder.Services.Configure<BinanceSettings>(builder.Configuration.GetSection("Binance"));
builder.Services.AddTransient<ICryptoDataFetcher, BinanceDataFetcher>();
builder.Services.AddTransient<IDataRepository, DataRepository>(); // Added this line
builder.Services.AddTransient<IMarkovChainCalculator, MarkovChainCalculator>();
builder.Services.AddTransient<IReversalCalculator, ReversalCalculator>();

builder.Services.AddControllers(); // Uncommented this line

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); // Added this line

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();