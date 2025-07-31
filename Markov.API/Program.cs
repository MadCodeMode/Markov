using HealthChecks.UI.Client;
using Markov.Services;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Repositories; 
using Markov.Services.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDbContext<MarkovDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.Configure<BinanceSettings>(builder.Configuration.GetSection("Binance"));
builder.Services.AddTransient<ICryptoDataFetcher, BinanceDataFetcher>();
builder.Services.AddTransient<IDataRepository, DataRepository>();
builder.Services.AddTransient<IMarkovChainCalculator, MarkovChainCalculator>();
builder.Services.AddTransient<IReversalCalculator, ReversalCalculator>();

builder.Services.AddControllers(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); 

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