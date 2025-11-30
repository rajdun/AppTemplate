using Application;
using Application.Common.Messaging;
using Domain;
using Hangfire;
using Infrastructure;
using WorkerService;


var builder = Host.CreateApplicationBuilder(args);
builder.AddTelemetry();
builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWorkerService(builder.Configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Use the instance of the recurring job manager to add or update the job.
    recurringJobManager.AddOrUpdate<OutboxProcessor>(
        "process-outbox",
        processor => processor.ProcessOutboxMessagesAsync(CancellationToken.None),
        "*/15 * * * * *"
    );
}

await host.RunAsync().ConfigureAwait(false);
