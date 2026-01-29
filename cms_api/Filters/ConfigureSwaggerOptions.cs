using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace cms_api.Filters
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        readonly IApiVersionDescriptionProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => this.provider = provider;

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc($"{description.GroupName}", CreateInfoForApiVersion(description));
            }
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = "AIA CMS API",
                Version = $"v{description.ApiVersion.ToString()}",
                Description = $"[version:{description.ApiVersion.ToString()}] | [Docker build time version : {DateTime.UtcNow.AddHours(6).AddMinutes(30).ToString("dd MMM yyyy hh:mm:ss tt")}]"
            };
            info.Description += "<ul>";
            if ($"{description.ApiVersion.ToString()}" == "1.0")
            {
                info.Description += $"<li>Staging</li>";
            }
            info.Description += "</ul>";
            if (description.IsDeprecated)
            {
                info.Description = $"version:{description.ApiVersion.ToString()} This API version has been deprecated.";
            }

            return info;
        }
    }

    /// <summary>
    /// //To replace the full name with namespace with the class name only
    /// </summary>
    public class NamespaceSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is null)
            {
                throw new System.ArgumentNullException(nameof(schema));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            schema.Title = context.Type.Name;
        }
    }
}
