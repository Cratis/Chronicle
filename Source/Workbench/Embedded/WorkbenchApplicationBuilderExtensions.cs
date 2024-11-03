// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Api.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Defines the extension methods for configuring the workbench for the <see cref="IApplicationBuilder"/> type.
/// </summary>
public static class WorkbenchApplicationBuilderExtensions
{
    /// <summary>
    /// Gets the default section path for the Chronicle configuration.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Chronicle", "Workbench"];

    /// <summary>
    /// Add the Cratis Chronicle Workbench as embedded.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to add to.</param>
    /// <param name="configureOptions">Optional <see cref="Action{T}"/> for configuring options.</param>
    /// <param name="configSection">Optional config section.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for continuation.</returns>
    public static WebApplicationBuilder AddCratisChronicleWorkbench(
        this WebApplicationBuilder builder,
        Action<ChronicleWorkbenchOptions>? configureOptions = default,
        string? configSection = default)
    {
        configSection ??= ConfigurationPath.Combine(DefaultSectionPaths);
        builder.Services.AddOptions(configureOptions);
        builder.Configuration.Bind(configSection);
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var workbenchOptions = context.Configuration.GetSection(configSection).Get<ChronicleWorkbenchOptions>();
            options.ListenLocalhost(workbenchOptions?.Port ?? ChronicleWorkbenchOptions.DefaultPort);
        });
        return builder;
    }

    /// <summary>
    /// Use the Cratis Chronicle Workbench as embedded.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static WebApplication UseCratisChronicleWorkbench(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ChronicleWorkbenchOptions>>();
        app.MapWhen(context => context.Connection.LocalPort == options.Value.Port, app =>
        {
            var rootType = typeof(WorkbenchApplicationBuilderExtensions);
            var rootResourceNamespace = $"{rootType.Namespace}.Files";
            var fileProvider = new ManifestEmbeddedFileProvider(rootType.Assembly, rootResourceNamespace);
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider
            });
            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = string.Empty
            };
            app.UseStaticFiles(staticFileOptions);

            app.UseEndpoints(endpoints => endpoints.MapFallbackToFile("index.html", staticFileOptions));
        });

        return app;
    }

    static OptionsBuilder<ChronicleWorkbenchOptions> AddOptions(this IServiceCollection services, Action<ChronicleWorkbenchOptions>? configure = default)
    {
        var builder = services
            .AddOptions<ChronicleWorkbenchOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure != default)
        {
            builder.Configure(configure);
        }

        return builder;
    }
}
