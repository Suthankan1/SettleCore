using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SettleCore.Api.Endpoints;
using SettleCore.Api.BackgroundTasks;
using SettleCore.Modules.Ledger.Infrastructure;
using SettleCore.Modules.Payments.Infrastructure;
using SettleCore.Modules.Reconciliation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<PaymentLedgerPostingWorkerOptions>()
    .Bind(builder.Configuration.GetSection("PaymentLedgerPostingWorker"))
    .Validate(static settings => settings.BatchSize > 0 &&
        settings.PollIntervalMilliseconds > 0 && settings.RetryDelaySeconds > 0,
        "Payment posting batch size, poll interval and retry delay must be positive.")
    .ValidateOnStart();
builder.Services.AddHostedService<PaymentLedgerPostingWorker>();

builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddReconciliationModule(builder.Configuration);
builder.Services.AddLedgerModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPaymentsEndpoints();
app.MapReconciliationEndpoints();
app.MapLedgerEndpoints();

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
