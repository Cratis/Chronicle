// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for using Cratis in an application.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
    /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
    public static IHostBuilder UseCratis(
        this IHostBuilder hostBuilder,
        Action<IClientBuilder>? configureDelegate = default,
        ILoggerFactory? loggerFactory = default)
    {
#pragma warning disable CA2000
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());

        hostBuilder.ConfigureServices((context, services) =>
        {
            var clientBuilder = new ClientBuilder(services, loggerFactory.CreateLogger<ClientBuilder>());
            configureDelegate?.Invoke(clientBuilder);
            clientBuilder.Build();
        });
        return hostBuilder;
    }
}
