// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers;

/// <summary>
/// Converter methods for working with <see cref="Observation.ObserverDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ObserverDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="ObserverDefinition">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ObserverDefinition"/>.</returns>
    public static ObserverDefinition ToSql(this Observation.ObserverDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            EventTypes = definition.EventTypes.Select(et => new EventTypeWithKeyExpression(et.Id, et.Generation, "$eventSourceId")).ToArray(),
            EventSequenceId = definition.EventSequenceId,
            Type = definition.Type,
            Owner = definition.Owner,
            IsReplayable = definition.IsReplayable,
        };

    /// <summary>
    /// Convert to <see cref="Observation.ObserverDefinition"/> from <see cref="ObserverDefinition"/>.
    /// </summary>
    /// <param name="schema"><see cref="ObserverDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Observation.ObserverDefinition"/>.</returns>
    public static Observation.ObserverDefinition ToKernel(this ObserverDefinition schema) =>
        new(
            schema.Id,
            schema.EventTypes.Select(et => new EventType(et.EventType, et.Generation)).ToArray(),
            schema.EventSequenceId,
            schema.Type,
            schema.Owner,
            schema.IsReplayable);
}
