// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Connections;
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
    /// Binds Chronicle settings and applies the named fallback after explicit configuration.
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
    {
        var unconfiguredConnectionStrings = new ConditionalWeakTable<TOptions, ChronicleConnectionString>();
        return builder
            .Configure(options =>
            {
                if (ReferenceEquals(options.ConnectionString, ChronicleConnectionString.Development))
                {
                    // A distinct default lets an explicit assignment of Development win as well.
                    var connectionString = new ChronicleConnectionString(options.ConnectionString.ToString());
                    unconfiguredConnectionStrings.Add(options, connectionString);
                    options.ConnectionString = connectionString;
                }
            })
            .BindConfiguration(configSectionPath)
            .PostConfigure<IConfiguration>((options, configuration) =>
            {
                if (unconfiguredConnectionStrings.TryGetValue(options, out var unconfiguredConnectionString) &&
                    ReferenceEquals(options.ConnectionString, unconfiguredConnectionString))
                {
                    var connectionString = configuration.GetConnectionString(connectionStringName);
                    options.ConnectionString = string.IsNullOrWhiteSpace(connectionString)
                        ? ChronicleConnectionString.Development
                        : new(connectionString);
                }
            });
    }
}
