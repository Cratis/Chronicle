// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Serverless;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle;

/// <summary>
/// Shares connection-string fallback and section binding between Chronicle hosts.
/// </summary>
internal static class ChronicleOptionsConfiguration
{
    const string EventStoreConfigurationKey = "Cratis:Chronicle:EventStore";

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
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>, ServerlessOptionsValidator<TOptions>>());
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

    /// <summary>
    /// Defaults the event store name when none is configured and requires one to be resolvable.
    /// </summary>
    /// <typeparam name="TOptions">The Chronicle client options type.</typeparam>
    /// <param name="builder">The options builder to configure.</param>
    /// <returns>The options builder for continuation.</returns>
    internal static OptionsBuilder<TOptions> WithDefaultEventStore<TOptions>(this OptionsBuilder<TOptions> builder)
        where TOptions : ChronicleClientOptions =>
        builder
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.EventStore.Value))
                {
                    options.EventStore = ResolveDefaultEventStore(options.ProgramIdentifier, Assembly.GetEntryAssembly()?.GetName().Name) ?? string.Empty;
                }
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.EventStore.Value),
                $"No event store name is configured. Set {EventStoreConfigurationKey} (environment variable Cratis__Chronicle__EventStore) or call AddCratisChronicle with a configureOptions that sets EventStore.");

    /// <summary>
    /// Resolves the default event store name from the program identifier or, failing that, the entry assembly name.
    /// </summary>
    /// <param name="programIdentifier">The configured program identifier.</param>
    /// <param name="entryAssemblyName">The entry assembly name, if there is one.</param>
    /// <returns>The default event store name, or <see langword="null"/> when none can be resolved.</returns>
    internal static string? ResolveDefaultEventStore(string programIdentifier, string? entryAssemblyName)
    {
        if (!string.IsNullOrWhiteSpace(programIdentifier) && programIdentifier != ChronicleOptions.DefaultProgramIdentifier)
        {
            return programIdentifier;
        }

        return string.IsNullOrWhiteSpace(entryAssemblyName) ? null : entryAssemblyName;
    }
}
