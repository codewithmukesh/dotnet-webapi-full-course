using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Ticketing.Api.Common;
using Ticketing.Api.Data;
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

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

builder.Services.AddDbContext<TicketingDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEventFeature();
builder.Services.AddBookingFeature();

builder.Services.AddOpenApi();

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

app.MapEventEndpoints();
app.MapBookingEndpoints();

app.Run();
