// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers;

/// <summary>
/// Converter methods for working with <see cref="Concepts.Observation.Reducers.ReducerDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ReducerDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="ReducerDefinition">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.Observation.Reducers.ReducerDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ReducerDefinition"/>.</returns>
    public static ReducerDefinition ToSql(this Concepts.Observation.Reducers.ReducerDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.Select(et => new EventTypeWithKeyExpression(et.EventType, et.EventType.Generation, et.Key.Expression)).ToArray(),
            ReadModel = definition.ReadModel,
            SinkType = definition.Sink.Type,
            SinkConfigurationId = definition.Sink.Configuration
        };

    /// <summary>
    /// Convert to <see cref="Concepts.Observation.Reducers.ReducerDefinition"/> from <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="schema"><see cref="ReducerDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Concepts.Observation.Reducers.ReducerDefinition"/>.</returns>
    public static Concepts.Observation.Reducers.ReducerDefinition ToKernel(this ReducerDefinition schema) =>
        new(
            schema.Id,
            schema.EventSequenceId,
            schema.EventTypes.Select(et => new Concepts.Observation.EventTypeWithKeyExpression(new EventType(et.EventType, et.Generation), et?.KeyExpression ?? PropertyExpression.NotSet)).ToArray(),
            schema.ReadModel,
            true,
            new SinkDefinition(schema.SinkConfigurationId, schema.SinkType),
            []);
}
