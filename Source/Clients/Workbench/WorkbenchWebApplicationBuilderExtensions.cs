// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench;

/// <summary>
/// Defines the extension methods for configuring the workbench for the <see cref="IApplicationBuilder"/> type.
/// </summary>
public static class WorkbenchWebApplicationBuilderExtensions
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
    public static WebApplicationBuilder UseCratisChronicleWorkbench(
        this WebApplicationBuilder builder,
        Action<ChronicleWorkbenchOptions>? configureOptions = default,
        string? configSection = default)
    {
        configSection ??= ConfigurationPath.Combine(DefaultSectionPaths);

        builder.Services
            .AddOptions(configureOptions)
            .BindConfiguration(configSection);
        builder.Services.AddHostedService<WebServer>();

        return builder;
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
