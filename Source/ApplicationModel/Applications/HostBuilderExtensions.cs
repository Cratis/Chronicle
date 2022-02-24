// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Aksio.Cratis;
using Aksio.Cratis.Applications;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Hosting;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Use Aksio defaults with the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <param name="microserviceId">Optional <see cref="MicroserviceId"/>.</param>
    /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// If the microservice identifier is not specified, it is assumed that the solution is
    /// not a microservice based solution, or that the  Kernel being connected to is for the
    /// microservice alone.
    /// </remarks>
    public static IHostBuilder UseAksio(this IHostBuilder builder, MicroserviceId? microserviceId = default, Action<IClientBuilder>? configureDelegate = default)
    {
        var loggerFactory = builder.UseDefaultLogging();
        var logger = loggerFactory.CreateLogger("Aksio setup");
        logger.SettingUpDefaults();

        var types = new Types("Aksio");
        types.RegisterTypeConvertersForConcepts();

        builder
            .UseMongoDB(types)
            .UseCratis(microserviceId ?? MicroserviceId.Unspecified, types, configureDelegate, loggerFactory)
            .ConfigureServices(_ =>
            {
                _
                .AddSingleton<ITypes>(types)
                .AddSingleton<ProviderFor<IServiceProvider>>(() => Internals.ServiceProvider!)
                .AddConfigurationObjects(types, searchSubPaths: new[] { "config" }, logger: logger)
                .AddControllersFromProjectReferencedAssembles(types)
                .AddSwaggerGen()
                .AddEndpointsApiExplorer()
                .AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.Providers.Add<BrotliCompressionProvider>();
                    options.Providers.Add<GzipCompressionProvider>();
                })
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize)

                // Temporarily adding this, due to a bug in .NET 6 (https://www.ingebrigtsen.info/2021/09/29/autofac-asp-net-core-6-hot-reload-debug-crash/):
                .AddRazorPages();

                _.AddMvc();
            })
            .UseDefaultDependencyInversion(types);

        return builder;
    }
}
