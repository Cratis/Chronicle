// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the named sinks of the kernel, and which of them read models and graphs store to by default.
/// </summary>
/// <remarks>
/// Without any configuration read models store to the kernel's own storage with the sink type the client chose, and graphs store
/// to the <see cref="ChronicleOptions.Neo4j"/> server, exactly as before named sinks existed.
/// </remarks>
public class SinksOptions
{
    /// <summary>
    /// Gets the name of the configuration read models store to when they name no sink. Empty keeps the kernel's own storage.
    /// </summary>
    public string ReadModels { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the configuration graphs store to when they name no sink. Empty keeps the <see cref="ChronicleOptions.Neo4j"/> server.
    /// </summary>
    public string Graphs { get; init; } = string.Empty;

    /// <summary>
    /// Gets the named sink configurations, by name. Names are matched without regard to case.
    /// </summary>
    public IDictionary<string, SinkConfiguration> Configurations { get; init; } = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Find a configuration by name, without regard to case.
    /// </summary>
    /// <param name="name">The name to find.</param>
    /// <param name="key">The name the configuration is configured by.</param>
    /// <param name="configuration">The <see cref="SinkConfiguration"/>, when found.</param>
    /// <returns>True if found.</returns>
    public bool TryGet(string name, out string key, out SinkConfiguration configuration)
    {
        foreach (var (candidate, value) in Configurations)
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase) && value is not null)
            {
                key = candidate;
                configuration = value;
                return true;
            }
        }

        key = string.Empty;
        configuration = null!;
        return false;
    }

    /// <summary>
    /// Validate the configuration, so the kernel refuses to start with sinks it cannot store to.
    /// </summary>
    /// <exception cref="InvalidSinkConfiguration">Thrown when a configuration or a default is invalid, naming it.</exception>
    public void Validate()
    {
        foreach (var (name, configuration) in Configurations)
        {
            Validate(name, configuration);
        }

        ValidateDefault(nameof(ReadModels), ReadModels, graph: false);
        ValidateDefault(nameof(Graphs), Graphs, graph: true);
    }

    static void Validate(string name, SinkConfiguration? configuration)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidSinkConfiguration(name, "a sink configuration must have a name.");
        }

        if (configuration is null)
        {
            throw new InvalidSinkConfiguration(name, "it has no settings.");
        }

        var type = SinkConfigurationRules.Normalize(configuration.Type)
            ?? throw new InvalidSinkConfiguration(name, $"the type '{configuration.Type}' is unknown. Use one of {string.Join(", ", SinkConfigurationRules.ReadModelTypes.Concat(SinkConfigurationRules.GraphTypes).Select(_ => _.Value))}.");

        if (type == WellKnownSinkTypes.Neo4j)
        {
            if (string.IsNullOrWhiteSpace(configuration.Server))
            {
                throw new InvalidSinkConfiguration(name, "a Neo4j sink needs a 'server'.");
            }

            if (!Uri.TryCreate(configuration.Server, UriKind.Absolute, out _))
            {
                throw new InvalidSinkConfiguration(name, $"the 'server' '{configuration.Server}' is not an absolute URI.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.ConnectionDetails))
        {
            throw new InvalidSinkConfiguration(name, $"a {type} sink needs 'connectionDetails'.");
        }
    }

    void ValidateDefault(string setting, string name, bool graph)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var qualified = $"sinks.{char.ToLowerInvariant(setting[0])}{setting[1..]}";
        if (!TryGet(name, out _, out var configuration))
        {
            throw new InvalidSinkConfiguration(name, $"'{qualified}' names it, but no sink configuration has that name.");
        }

        var type = SinkConfigurationRules.Normalize(configuration.Type)!;
        if (!SinkConfigurationRules.Serves(type, graph))
        {
            var reason = graph
                ? $"'{qualified}' names it, but its type '{type}' cannot store graphs."
                : $"'{qualified}' names it, but its type '{type}' only stores graphs.";
            throw new InvalidSinkConfiguration(name, reason);
        }
    }
}
