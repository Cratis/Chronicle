// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="ReactorDefinition"/> representations.
/// </summary>
public static class ReactorDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to a MongoDB <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel reactor definition.</param>
    /// <returns>The MongoDB reactor definition.</returns>
    public static ReactorDefinition ToMongoDB(this Concepts.Observation.Reactors.ReactorDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.EventType.ToString(),
                et => et.Key.ToString()),
            IsReplayable = definition.IsReplayable,
            Categories = definition.Categories ?? []
        };

    /// <summary>
    /// Converts a MongoDB <see cref="ReactorDefinition"/> to a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB reactor definition.</param>
    /// <returns>The Kernel reactor definition.</returns>
    public static Concepts.Observation.Reactors.ReactorDefinition ToKernel(this ReactorDefinition definition) =>
        new(
            definition.Id,
            definition.Owner,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => new EventTypeWithKeyExpression(EventType.Parse(kvp.Key), (PropertyExpression)kvp.Value)),
            definition.IsReplayable,
            definition.Categories);
}
