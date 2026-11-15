// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Holds the rules for which sink types a named sink configuration applies to.
/// </summary>
public static class SinkConfigurationRules
{
    /// <summary>
    /// Gets the sink types a named configuration can have for read models.
    /// </summary>
    public static readonly IReadOnlyCollection<SinkTypeId> ReadModelTypes = [WellKnownSinkTypes.MongoDB, WellKnownSinkTypes.SQL];

    /// <summary>
    /// Gets the sink types a named configuration can have for graphs.
    /// </summary>
    public static readonly IReadOnlyCollection<SinkTypeId> GraphTypes = [WellKnownSinkTypes.Neo4j];

    /// <summary>
    /// Check whether a read model with a sink type stores anywhere a sink configuration could name.
    /// </summary>
    /// <param name="type">The <see cref="SinkTypeId"/> the read model declares.</param>
    /// <returns>False for passive read models (<see cref="SinkTypeId.None"/>) and event targets; true otherwise.</returns>
    public static bool CanBeNamed(SinkTypeId type) => !Same(type, SinkTypeId.None) && !Same(type, WellKnownSinkTypes.EventSequence);

    /// <summary>
    /// Check whether a sink type is one of the graph sink types.
    /// </summary>
    /// <param name="type">The <see cref="SinkTypeId"/> to check.</param>
    /// <returns>True if it stores graphs.</returns>
    public static bool IsGraph(SinkTypeId type) => IsOneOf(type, GraphTypes) || Same(type, WellKnownSinkTypes.InMemoryGraph);

    /// <summary>
    /// Check whether a sink type can be the type of a named configuration for the kind of read model.
    /// </summary>
    /// <param name="type">The <see cref="SinkTypeId"/> of the configuration.</param>
    /// <param name="graph">Whether the read model is a graph.</param>
    /// <returns>True if it can.</returns>
    public static bool Serves(SinkTypeId type, bool graph) => IsOneOf(type, graph ? GraphTypes : ReadModelTypes);

    /// <summary>
    /// Get the well-known spelling of a sink type, matched without regard to case.
    /// </summary>
    /// <param name="type">The type as configured.</param>
    /// <returns>The well-known <see cref="SinkTypeId"/>, or null if the type cannot be configured by name.</returns>
    public static SinkTypeId? Normalize(string? type) =>
        ReadModelTypes.Concat(GraphTypes).FirstOrDefault(_ => string.Equals(_.Value, type, StringComparison.OrdinalIgnoreCase));

    static bool IsOneOf(SinkTypeId type, IEnumerable<SinkTypeId> types) => types.Any(_ => Same(_, type));

    static bool Same(SinkTypeId first, SinkTypeId second) => string.Equals(first.Value, second.Value, StringComparison.OrdinalIgnoreCase);
}
