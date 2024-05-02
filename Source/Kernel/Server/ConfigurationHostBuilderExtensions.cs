// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up configuration for host.
/// </summary>
public static class ConfigurationHostBuilderExtensions
{
    static ConfigurationHostBuilderExtensions()
    {
        Configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
              .AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true)
              .Build();
    }

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> object configured using the "<see cref="UseConfiguration"/>.
    /// </summary>
    public static IConfiguration Configuration { get; }

    /// <summary>
    /// Use default configuration.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to use with.</param>
    /// <returns><see cref="IHostBuilder"/> for continuation.</returns>
    public static IHostBuilder UseConfiguration(this IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(_ =>
        {
            _.Sources.Clear();
            _.AddConfiguration(Configuration);
        });

        return builder;
    }
}
