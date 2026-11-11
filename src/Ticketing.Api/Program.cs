using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using Ticketing.Api.Common;
using Ticketing.Api.Data;
using Ticketing.Api.Features.Auth;
using Ticketing.Api.Features.Bookings;
using Ticketing.Api.Features.Events;

var builder = WebApplication.CreateBuilder(args);

// On by default only in Development. Turn it on everywhere so lifetime bugs fail at boot.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddOptions<TicketingOptions>()
    .BindConfiguration(TicketingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// PostgreSQL
var databaseConnection = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

builder.Services.AddDbContext<TicketingDbContext>(options => options.UseNpgsql(databaseConnection));

// Redis: ONE shared connection for the cache and the health check.
var redisOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' is missing."));
redisOptions.AbortOnConnectFail = false; // boot even if Redis is down; /health/ready reports it
redisOptions.ConnectTimeout = 2000;      // ms. The 5 s default makes EVERY request wait 5 s when Redis is down.
redisOptions.SyncTimeout = 1000;
redisOptions.AsyncTimeout = 1000;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddStackExchangeRedisCache(_ => { });
builder.Services.AddOptions<RedisCacheOptions>()
    .Configure<IConnectionMultiplexer>((options, redis) => options.ConnectionMultiplexerFactory = () => Task.FromResult(redis));

// HybridCache: memory (L1) + Redis (L2). It picks up Redis as the registered IDistributedCache.
builder.Services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromSeconds(30),           // Redis
    LocalCacheExpiration = TimeSpan.FromSeconds(10), // this server's memory
});

builder.Services.AddSingleton(TimeProvider.System);

// Enums as "Organizer", not 1, both in requests and responses (and in the OpenAPI schema).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTicketingRateLimiting();
builder.Services.AddTicketingHealthChecks();

builder.Services.AddAuthFeature();
builder.Services.AddEventFeature();
builder.Services.AddBookingFeature();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

// First in the pipeline, so it catches everything after it. Same behaviour in every environment.
app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// After authentication, so the booking limiter can partition by user id.
app.UseRateLimiter();

app.MapTicketingHealthChecks();
app.MapAuthEndpoints();
app.MapEventEndpoints();
app.MapBookingEndpoints();

app.Run();
