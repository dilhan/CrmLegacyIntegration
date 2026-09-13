using System.Reflection;
using CrmLegacyIntegration.Core.Security;
using CrmLegacyIntegration.Functions.OpenApi;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Jwt__SigningKey / Jwt__RequiredRole in local.settings.json's
        // Values bind here as the "Jwt" section (double-underscore is the
        // standard flattening convention for environment-variable-backed
        // configuration).
        var jwtOptions = context.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                          ?? new JwtOptions();
        services.AddSingleton(jwtOptions);
        services.AddSingleton<JwtAccessTokenIssuer>();
        services.AddSingleton<JwtBearerAuthenticator>();

        services.AddSingleton<IOpenApiConfigurationOptions, OpenApiConfigurationOptions>();
        services.AddSingleton<IOpenApiCustomUIOptions>(
            _ => new SwaggerAutoAuthorizeScript(Assembly.GetExecutingAssembly()));
    })
    .Build();

host.Run();
