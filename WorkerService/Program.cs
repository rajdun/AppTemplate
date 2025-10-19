using Application;
using Domain;
using Hangfire;
using Infrastructure;
using WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDomain();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWorkerService(builder.Configuration);

var host = builder.Build();

await host.RunAsync();