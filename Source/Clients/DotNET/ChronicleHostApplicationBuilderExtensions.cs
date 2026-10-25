// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for using Cratis Chronicle with <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class ChronicleHostApplicationBuilderExtensions
{
    /// <summary>
    /// Gets the default configuration section path for Chronicle settings.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Chronicle"];

    /// <summary>
    /// Configures the Chronicle client for a .NET host application.
    /// </summary>
    /// <param name="builder"><see cref="IHostApplicationBuilder"/> to configure Chronicle on.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="ChronicleClientOptions"/>.</param>
    /// <param name="configSection">Optional configuration section path override. Defaults to <c language="csharp">Cratis:Chronicle</c>.</param>
    /// <param name="configure">Optional callback for configuring structural dependencies via <see cref="IChronicleBuilder"/>.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for continuation.</returns>
    public static IHostApplicationBuilder AddCratisChronicle(
        this IHostApplicationBuilder builder,
        Action<ChronicleClientOptions>? configureOptions = default,
        string? configSection = default,
        Action<IChronicleBuilder>? configure = default)
        => builder.AddCratisChronicle(configureOptions, configSection, configure, "chronicle");

    /// <summary>
    /// Configures the Chronicle client using a named connection string as the fallback endpoint.
    /// </summary>
    /// <param name="builder"><see cref="IHostApplicationBuilder"/> to configure Chronicle on.</param>
    /// <param name="configureOptions">Optional callback whose connection string overrides configuration.</param>
    /// <param name="configSection">Optional configuration section path override. Defaults to <c language="csharp">Cratis:Chronicle</c>.</param>
    /// <param name="configure">Optional callback for configuring structural dependencies via <see cref="IChronicleBuilder"/>.</param>
    /// <param name="connectionStringName">Name under <c language="csharp">ConnectionStrings</c>, typically the Aspire resource name.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for continuation.</returns>
    public static IHostApplicationBuilder AddCratisChronicle(
        this IHostApplicationBuilder builder,
        Action<ChronicleClientOptions>? configureOptions,
        string? configSection,
        Action<IChronicleBuilder>? configure,
        string connectionStringName)
    {
        ConceptTypeConvertersRegistrar.EnsureFor(typeof(ChronicleHostApplicationBuilderExtensions).Assembly);
        ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

        var configSectionPath = configSection ?? ConfigurationPath.Combine(DefaultSectionPaths);

        var chronicleBuilder = new ChronicleBuilder(builder.Services, builder.Configuration, DefaultClientArtifactsProvider.Default);

        builder.Services.AddChronicleClientOptions(configureOptions, configSectionPath, connectionStringName);

        configure?.Invoke(chronicleBuilder);

        builder.Services.AddStandaloneChronicleClient(chronicleBuilder);

        return builder;
    }

    static void AddChronicleClientOptions(
        this IServiceCollection services,
        Action<ChronicleClientOptions>? configure,
        string configSectionPath,
        string connectionStringName)
    {
        services
            .AddOptions<ChronicleClientOptions>()
            .WithChronicleConfiguration(configSectionPath, connectionStringName)
            .Configure(options => configure?.Invoke(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
