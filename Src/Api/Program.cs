using Api;
using Api.Common;
using Api.Resources;
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
var command = args.FirstOrDefault();

// Presentation and Domain are always registered (lightweight, no external dependencies)
builder.Services.AddDomain();
builder.Services.AddPresentation(builder.Configuration);

if (command is "get-api-documentation")
{
    // Only auth scheme provider needed for OpenApiDocumentTransformer
    builder.Services.AddAuthentication();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
}
else
{
    builder.AddTelemetry();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
}

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    var validate = command is null && context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = validate;
    options.ValidateScopes = validate;
});

var app = builder.Build();

// CLI commands
if (command is "migrate")
{
    Console.WriteLine(ApiResources.Applying_database_migrations___);
    var scope = app.Services.CreateAsyncScope();
    await using (scope.ConfigureAwait(false))
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }
    Console.WriteLine(ApiResources.Migrations_applied_successfully__Exiting_);
    return;
}

if (command is "get-api-documentation")
{
    app.MapOpenApi();
    app.MapCarter();

    await app.StartAsync().ConfigureAwait(false);

    using var httpClient = new HttpClient();
    var url = new Uri(new Uri(app.Urls.First()), "openapi/v1.json");
    var json = await httpClient.GetStringAsync(url).ConfigureAwait(false);

    var outputPath = args.ElementAtOrDefault(1) ?? "openapi.json";
    await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
    Console.WriteLine($"OpenAPI document written to: {Path.GetFullPath(outputPath)}");

    await app.StopAsync().ConfigureAwait(false);
    return;
}

// Runtime middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseCors(app.Configuration.GetValue<string>("Cors:PolicyName") ?? "DefaultCorsPolicy");

var supportedCultures = new[] { "pl-PL", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter(app.Configuration)]
});

app.MapCarter();

try
{
    await app.RunAsync().ConfigureAwait(false);
}
#pragma warning disable CA1031
catch (Exception ex)
#pragma warning restore CA1031
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
