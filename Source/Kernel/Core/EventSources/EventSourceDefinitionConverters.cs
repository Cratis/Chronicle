// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Converts between the wire representation of event source definitions and the Kernel concepts.
/// </summary>
internal static class EventSourceDefinitionConverters
{
    /// <summary>
    /// Converts a wire definition into the Kernel concept.
    /// </summary>
    /// <param name="definition">The wire definition.</param>
    /// <returns>The Kernel <see cref="EventSourceDefinition"/>.</returns>
    internal static EventSourceDefinition ToConcept(this Contracts.EventSources.EventSourceDefinition definition) =>
        new(
            definition.Name,
            definition.Description ?? string.Empty,
            definition.Owner == Contracts.EventSources.EventSourceOwner.None ? EventSourceOwner.Client : (EventSourceOwner)(int)definition.Owner,
            (ConcurrencyDimensions)(int)definition.Concurrency,
            [.. (definition.Streams ?? []).Select(stream => new EventStreamDefinition(
                stream.Name,
                stream.Description ?? string.Empty,
                (ConcurrencyDimensions)(int)stream.Concurrency))]);

    /// <summary>
    /// Converts Kernel definitions into the read model the event source queries answer with.
    /// </summary>
    /// <param name="definitions">The Kernel definitions.</param>
    /// <returns>The definitions as read models.</returns>
    internal static IEnumerable<EventSourceDetails> ToReadModel(this IEnumerable<EventSourceDefinition> definitions) =>
        [.. definitions.Select(definition => new EventSourceDetails(
            definition.Name.Value,
            definition.Name.Value,
            definition.Description.Value,
            (Contracts.EventSources.EventSourceOwner)(int)definition.Owner,
            (Contracts.EventSources.ConcurrencyDimensions)(int)definition.Concurrency,
            [.. definition.Streams.Select(stream => new Contracts.EventSources.EventStreamDefinition
            {
                Name = stream.Name.Value,
                Description = stream.Description.Value,
                Concurrency = (Contracts.EventSources.ConcurrencyDimensions)(int)stream.Concurrency
            })]))];
}
