using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SettleCore.Api.Endpoints;
using SettleCore.Modules.Payments.Infrastructure;
using SettleCore.Modules.Reconciliation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddReconciliationModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPaymentsEndpoints();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = healthCheck =>
            healthCheck.Tags.Contains("ready")
    });

app.Run();

public partial class Program
{
}