using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AGTec.Services.ServiceDefaults.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly string _apiTitle;
    private readonly string _apiDescription;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, string apiTitle = "API", string apiDescription = "API Service")
    {
        _provider = provider;
        _apiTitle = apiTitle;
        _apiDescription = apiDescription;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = _apiTitle,
            Version = description.ApiVersion.ToString(),
            Description = _apiDescription
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}
