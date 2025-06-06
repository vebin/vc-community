using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DeveloperTools;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Swagger;

namespace VirtoCommerce.Platform.Web.Swagger
{
    public static class SwaggerServiceCollectionExtensions
    {
        public static string PlatformDocName => "VirtoCommerce.Platform";
        public static string PlatformUIDocName => "PlatformUI";

        /// <summary>
        /// Register swagger documents generator
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="useAllOfToExtendReferenceSchemas"></param>
        public static void AddSwagger(this IServiceCollection services, IConfiguration configuration, bool useAllOfToExtendReferenceSchemas)
        {
            var section = configuration.GetSection("VirtoCommerce:Swagger");
            var swaggerOptions = new SwaggerPlatformOptions();
            section.Bind(swaggerOptions);
            services.AddOptions<SwaggerPlatformOptions>().Bind(section).ValidateDataAnnotations();

            if (!swaggerOptions.Enable)
            {
                return;
            }

            var provider = services.BuildServiceProvider();
            var modules = provider.GetService<IModuleCatalog>().Modules.OfType<ManifestModuleInfo>().Where(m => m.ModuleInstance != null).ToArray();

            services.AddSwaggerGen(c =>
            {
                var platformInfo = new OpenApiInfo
                {
                    Title = "Virto Commerce Solution REST API Documentation",
                    Version = "v1",
                    TermsOfService = new Uri("https://virtocommerce.com/terms"),
                    Description = "Virto Commerce provides API documentation in two formats, JSON and YAML, with schema files generated as swagger.json and swagger.yaml. To ensure secure access, authorization filters can be applied using a specific key to grant access. This allows authorized users to securely interact with the API and access the necessary resources while maintaining confidentiality and data integrity.",
                    Contact = new OpenApiContact
                    {
                        Email = "support@virtocommerce.com",
                        Name = "Virto Commerce",
                        Url = new Uri("https://virtocommerce.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Virto Commerce Open Software License 3.0",
                        Url = new Uri("https://virtocommerce.com/open-source-license")
                    }
                };

                c.SwaggerDoc(PlatformDocName, platformInfo);
                c.SwaggerDoc(PlatformUIDocName, platformInfo);

                foreach (var module in modules)
                {
                    c.SwaggerDoc(module.ModuleName, new OpenApiInfo { Title = $"{module.Id}", Version = "v1" });
                }

                c.TagActionsBy(api => [api.GetModuleName(provider)]);
                c.IgnoreObsoleteActions();
                c.DocumentFilter<ExcludeRedundantDepsFilter>();
                // This temporary filter removes broken "application/*+json" content-type.
                // It seems it's some openapi/swagger bug, because Autorest fails.
                c.OperationFilter<ConsumeFromBodyFilter>();
                c.OperationFilter<FileResponseTypeFilter>();
                c.OperationFilter<OptionalParametersFilter>();
                c.OperationFilter<ArrayInQueryParametersFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<TagsFilter>();
                c.OperationFilter<OpenIDEndpointDescriptionFilter>();
                c.SchemaFilter<EnumSchemaFilter>();
                c.SchemaFilter<SwaggerIgnoreFilter>();
                c.MapType<object>(() => new OpenApiSchema { Type = "object" });
                c.AddModulesXmlComments(provider);
                c.CustomOperationIds(apiDesc =>
                    apiDesc.TryGetMethodInfo(out var methodInfo) ? $"{((ControllerActionDescriptor)apiDesc.ActionDescriptor).ControllerName}_{methodInfo.Name}" : null);
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "OAuth2 Resource Owner Password Grant flow",
                    Flows = new OpenApiOAuthFlows
                    {
                        Password = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri("/connect/token", UriKind.Relative)
                        }
                    },
                });

                c.DocInclusionPredicate((docName, apiDesc) => DocInclusionPredicateCustomStrategy(modules, docName, apiDesc));
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                c.EnableAnnotations(enableAnnotationsForInheritance: true, enableAnnotationsForPolymorphism: true);

                if (useAllOfToExtendReferenceSchemas)
                {
                    c.UseAllOfToExtendReferenceSchemas();
                }
            });

            // Unfortunately, we can't use .CustomSchemaIds, because it changes schema ids for all documents (impossible to change ids depending on document name).
            // But we need this, because PlatformUI document should contain ref schema ids as type.FullName to avoid conflict with same type names in different modules.
            // As a solution we use custom swagger generator that catches document name and generates schema ids depending on it.
            services.AddTransient<IAsyncSwaggerProvider, CustomSwaggerGenerator>();

            //This is important line switches the SwaggerGenerator to use the Newtonsoft contract resolver that uses the globally registered PolymorphJsonContractResolver
            //to propagate up to the resulting OpenAPI schema the derived types instead of base domain types
            services.AddSwaggerGenNewtonsoftSupport();
        }

        private static bool DocInclusionPredicateCustomStrategy(ManifestModuleInfo[] modules, string docName, ApiDescription apiDesc)
        {
            // It's a UI endpoint, return all to correctly build swagger UI page
            if (docName.EqualsIgnoreCase(PlatformUIDocName))
            {
                return true;
            }

            // It's a platform endpoint.
            var currentAssembly = ((ControllerActionDescriptor)apiDesc.ActionDescriptor).ControllerTypeInfo.Assembly;
            if (docName.EqualsIgnoreCase(PlatformDocName) && currentAssembly.FullName?.StartsWith(docName) == true)
            {
                return true;
            }

            // It's a module endpoint. 
            var module = modules.FirstOrDefault(m => m.ModuleName.EqualsIgnoreCase(docName));
            return module != null && module.Assembly == currentAssembly;
        }

        /// <summary>
        /// Enable endpoints for swagger documents and UI
        /// </summary>
        /// <param name="applicationBuilder"></param>
        public static void UseSwagger(this IApplicationBuilder applicationBuilder)
        {
            var swaggerOptions = applicationBuilder.ApplicationServices.GetRequiredService<IOptions<SwaggerPlatformOptions>>().Value;
            if (!swaggerOptions.Enable)
            {
                return;
            }

            applicationBuilder.UseSwagger(c =>
            {
                c.RouteTemplate = "docs/{documentName}/swagger.{json|yaml}";
                c.PreSerializeFilters.Add((_, _) =>
                {
                });

            });

            var modules = applicationBuilder.ApplicationServices.GetService<IModuleCatalog>().Modules.OfType<ManifestModuleInfo>().Where(m => m.ModuleInstance != null).ToArray();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            applicationBuilder.UseSwaggerUI(c =>
            {
                // Json Format Support 
                c.SwaggerEndpoint($"./{PlatformUIDocName}/swagger.json", PlatformUIDocName);
                c.SwaggerEndpoint($"./{PlatformDocName}/swagger.json", PlatformDocName);

                foreach (var moduleId in modules.OrderBy(m => m.Id).Select(m => m.Id))
                {
                    c.SwaggerEndpoint($"./{moduleId}/swagger.json", moduleId);
                }

                c.RoutePrefix = "docs";
                c.EnableValidator();
                c.IndexStream = () =>
                {
                    var type = typeof(Startup).GetTypeInfo().Assembly
                        .GetManifestResourceStream("VirtoCommerce.Platform.Web.wwwroot.swagger.index.html");
                    return type;
                };
                c.DocumentTitle = "Virto Commerce Solution REST API Documentation";
                c.InjectStylesheet("/swagger/vc.css");
                c.ShowExtensions();
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
            });

            var toolRegistrar = applicationBuilder.ApplicationServices.GetService<IDeveloperToolRegistrar>();
            toolRegistrar.RegisterDeveloperTool(new()
            {
                Name = "Swagger",
                Url = "/docs/index.html",
                SortOrder = 30,
            });
        }


        private static string GetModuleName(this ApiDescription api, ServiceProvider serviceProvider)
        {
            var moduleCatalog = serviceProvider.GetRequiredService<ILocalModuleCatalog>();

            // ------
            // Lifted from ApiDescriptionExtensions
            var actionDescriptor = api.GetProperty<ControllerActionDescriptor>();

            if (actionDescriptor == null)
            {
                actionDescriptor = api.ActionDescriptor as ControllerActionDescriptor;
                api.SetProperty(actionDescriptor);
            }
            // ------

            var moduleAssembly = actionDescriptor?.ControllerTypeInfo.Assembly ?? Assembly.GetExecutingAssembly();
            var module = moduleCatalog.Modules.FirstOrDefault(m => m.ModuleInstance != null && m.Assembly == moduleAssembly);

            return module?.ModuleName ?? "Platform";
        }

        /// <summary>
        /// Add Comments/Descriptions from XML-files in the ApiDescription
        /// </summary>
        private static void AddModulesXmlComments(this SwaggerGenOptions options, ServiceProvider serviceProvider)
        {
            var localStorageModuleCatalogOptions = serviceProvider.GetService<IOptions<LocalStorageModuleCatalogOptions>>().Value;

            var xmlCommentsDirectoryPaths = new[]
            {
                localStorageModuleCatalogOptions.ProbingPath,
                AppContext.BaseDirectory
            };

            foreach (var path in xmlCommentsDirectoryPaths)
            {
                var xmlComments = Directory.GetFiles(path, "*.XML");
                foreach (var xmlComment in xmlComments)
                {
                    options.IncludeXmlComments(xmlComment);
                }
            }
        }
    }
}
