// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle;

/// <summary>
/// Shares connection-string fallback and section binding between Chronicle hosts.
/// </summary>
internal static class ChronicleOptionsConfiguration
{
    /// <summary>
    /// Applies the named connection string before binding explicit Chronicle settings.
    /// </summary>
    /// <typeparam name="TOptions">The Chronicle options type.</typeparam>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="configSectionPath">The Chronicle configuration section path.</param>
    /// <param name="connectionStringName">The name of the fallback connection string.</param>
    /// <returns>The options builder for continuation.</returns>
    internal static OptionsBuilder<TOptions> WithChronicleConfiguration<TOptions>(
        this OptionsBuilder<TOptions> builder,
        string configSectionPath,
        string connectionStringName)
        where TOptions : ChronicleOptions
        => builder
            .Configure<IConfiguration>((options, configuration) =>
            {
                var explicitConnectionString = configuration[$"{configSectionPath}:{nameof(ChronicleOptions.ConnectionString)}"];
                var connectionString = configuration.GetConnectionString(connectionStringName);
                if (explicitConnectionString is null && !string.IsNullOrWhiteSpace(connectionString))
                {
                    options.ConnectionString = new(connectionString);
                }
            })
            .BindConfiguration(configSectionPath);
}
