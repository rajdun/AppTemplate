using Api.Common.Dto;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Api.Common;

public class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly OpenApiSettings _settings;

    public OpenApiDocumentTransformer(OpenApiSettings settings)
    {
        _settings = settings;
    }
    
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.DocumentName != "internal") return Task.CompletedTask;
        
        document.Servers.Clear();

        if (_settings.Servers.Count > 0)
        {
            foreach (var s in _settings.Servers.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
            {
                document.Servers.Add(new OpenApiServer
                {
                    Url = s.Url,
                    Description = s.Description
                });
            }
        }
        else
        {
            // fallback if no config provided
            document.Servers.Add(new OpenApiServer { Url = "http://localhost:8080", Description = "Local docker" });
            document.Servers.Add(new OpenApiServer { Url = "https://localhost:5045", Description = "Local kestrel" });
        }
        
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        document.SecurityRequirements ??= new List<OpenApiSecurityRequirement>();
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
        return Task.CompletedTask;
    }
}
