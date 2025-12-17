// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reducers;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="ReducerDefinition"/> representations.
/// </summary>
public static class ReducerDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Reducers.ReducerDefinition"/> to a MongoDB <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel reducer definition.</param>
    /// <returns>The MongoDB reducer definition.</returns>
    public static ReducerDefinition ToMongoDB(this Concepts.Observation.Reducers.ReducerDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.EventType.ToString(),
                et => et.Key.ToString()),
            ReadModel = definition.ReadModel,
            IsActive = definition.IsActive,
            Sink = definition.Sink,
            Categories = definition.Categories
        };

    /// <summary>
    /// Converts a MongoDB <see cref="ReducerDefinition"/> to a Kernel <see cref="Concepts.Observation.Reducers.ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB reducer definition.</param>
    /// <returns>The Kernel reducer definition.</returns>
    public static Concepts.Observation.Reducers.ReducerDefinition ToKernel(this ReducerDefinition definition) =>
        new(
            definition.Id,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => new EventTypeWithKeyExpression(EventType.Parse(kvp.Key), (PropertyExpression)kvp.Value)),
            definition.ReadModel,
            definition.IsActive,
            definition.Sink,
            definition.Categories);
}
