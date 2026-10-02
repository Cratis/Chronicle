// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Converts event source definitions to their contract representation.
/// </summary>
internal static class EventSourceConverters
{
    /// <summary>
    /// Converts <see cref="ConcurrencyDimensions"/> to the contract representation.
    /// </summary>
    /// <param name="dimensions">The dimensions to convert.</param>
    /// <returns>The contract dimensions.</returns>
    internal static Contracts.EventSources.ConcurrencyDimensions ToContract(this ConcurrencyDimensions dimensions) =>
        (Contracts.EventSources.ConcurrencyDimensions)(int)dimensions;

    /// <summary>
    /// Converts an <see cref="EventSourceDefinition"/> to the contract representation.
    /// </summary>
    /// <param name="definition">The definition to convert.</param>
    /// <returns>The contract definition.</returns>
    internal static Contracts.EventSources.EventSourceDefinition ToContract(this EventSourceDefinition definition) => new()
    {
        Name = definition.Name,
        Description = definition.Description,
        Owner = Contracts.EventSources.EventSourceOwner.Client,
        Concurrency = definition.Concurrency.ToContract(),
        Streams = [.. definition.Streams.Select(stream => new Contracts.EventSources.EventStreamDefinition
        {
            Name = stream.Name,
            Description = stream.Description,
            Concurrency = stream.Concurrency.ToContract()
        })]
    };
}
