// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Provides conversion methods for converting between <see cref="Chronicle.Storage.Observation.ObserverDefinition"/> and its MongoDB representation.
/// </summary>
public static class ObserverDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="Chronicle.Storage.Observation.ObserverDefinition"/> to its MongoDB representation.
    /// </summary>
    /// <param name="definition">The Kernel observer definition.</param>
    /// <returns>The MongoDB representation of the observer definition.</returns>
    public static ObserverDefinition ToMongoDB(this Chronicle.Storage.Observation.ObserverDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.ToString(),
                et => "$eventSourceId"),
            EventSequenceId = definition.EventSequenceId,
            Type = definition.Type,
            Owner = definition.Owner,
            IsReplayable = definition.IsReplayable
        };

    /// <summary>
    /// Converts a MongoDB representation of an observer definition to its Kernel representation.
    /// </summary>
    /// <param name="definition">The MongoDB observer definition.</param>
    /// <returns>The Kernel representation of the observer definition.</returns>
    public static Chronicle.Storage.Observation.ObserverDefinition ToKernel(this ObserverDefinition definition) =>
        new(
            definition.Id,
            definition.EventTypes.Select(kvp => EventType.Parse(kvp.Key)).ToArray(),
            definition.EventSequenceId,
            definition.Type,
            definition.Owner,
            definition.IsReplayable);

    /// <summary>
    /// Converts a collection of MongoDB representations of observer definitions to their Kernel representations.
    /// </summary>
    /// <param name="definitions">The MongoDB observer definitions.</param>
    /// <returns>The Kernel representations of the observer definitions.</returns>
    public static IEnumerable<Chronicle.Storage.Observation.ObserverDefinition> ToKernel(this IEnumerable<ObserverDefinition> definitions) =>
        definitions.Select(ToKernel).ToArray();
}
