// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for using Cratis.Chronicle in an application.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
    public static IHostBuilder AddCratisChronicle(
        this IHostBuilder hostBuilder,
        ILoggerFactory? loggerFactory = default)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000 // Dispose objects before losing scope

        hostBuilder.ConfigureServices((context, services) =>
        {
        });
        return hostBuilder;
    }
}
