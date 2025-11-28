using System.Reflection;
using Api;
using Api.Common;
using Application;
using Carter;
using Domain;
using Hangfire;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var isRuntime = Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider";

builder.AddTelemetry();
builder.Services.AddDomain();
if (isRuntime)
{
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
}

builder.Services.AddPresentation(builder.Configuration);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateOnBuild = !isRuntime;
    options.ValidateScopes = !isRuntime;
});

var app = builder.Build();

if (args.Contains("migrate"))
{
    Console.WriteLine("Applying database migrations...");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    Console.WriteLine("Migrations applied successfully. Exiting.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(app.Configuration.GetValue<string>("Cors:PolicyName") ?? "DefaultCorsPolicy");
app.UseExceptionHandler();

var supportedCultures = new[] { "pl-PL", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

if (isRuntime)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

app.MapCarter();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}