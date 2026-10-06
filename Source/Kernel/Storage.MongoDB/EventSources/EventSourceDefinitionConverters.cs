// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSources;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB event source representations.
/// </summary>
public static class EventSourceDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel definition to its MongoDB document.
    /// </summary>
    /// <param name="definition">The Kernel definition.</param>
    /// <returns>The MongoDB document.</returns>
    public static EventSourceDefinition ToMongoDB(this Concepts.EventSources.EventSourceDefinition definition) =>
        new()
        {
            Id = definition.Name.Value,
            Description = definition.Description.Value,
            Owner = definition.Owner,
            Concurrency = definition.Concurrency,
            Streams = [.. definition.Streams.Select(stream => new EventStreamDefinition
            {
                Name = stream.Name.Value,
                Description = stream.Description.Value,
                Concurrency = stream.Concurrency
            })]
        };

    /// <summary>
    /// Converts a MongoDB document to a Kernel definition.
    /// </summary>
    /// <param name="definition">The MongoDB document.</param>
    /// <returns>The Kernel definition.</returns>
    public static Concepts.EventSources.EventSourceDefinition ToKernel(this EventSourceDefinition definition) =>
        new(
            definition.Id,
            definition.Description ?? string.Empty,
            definition.Owner,
            definition.Concurrency,
            [.. (definition.Streams ?? []).Select(stream => new Concepts.EventSources.EventStreamDefinition(
                stream.Name,
                stream.Description ?? string.Empty,
                stream.Concurrency))]);
}
