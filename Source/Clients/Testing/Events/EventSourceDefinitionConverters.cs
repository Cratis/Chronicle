// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.EventSources;
using KernelEventSources = KernelConcepts::Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Converts client event source definitions to the Kernel concepts the in-memory storage holds.
/// </summary>
internal static class EventSourceDefinitionConverters
{
    /// <summary>
    /// Converts a client definition to its Kernel concept.
    /// </summary>
    /// <param name="definition">The client <see cref="EventSourceDefinition"/>.</param>
    /// <returns>The Kernel definition.</returns>
    internal static KernelEventSources::EventSourceDefinition ToKernel(this EventSourceDefinition definition) =>
        new(
            definition.Name,
            definition.Description,
            KernelEventSources::EventSourceOwner.Client,
            (KernelEventSources::ConcurrencyDimensions)(int)definition.Concurrency,
            [.. definition.Streams.Select(stream => new KernelEventSources::EventStreamDefinition(
                stream.Name,
                stream.Description,
                (KernelEventSources::ConcurrencyDimensions)(int)stream.Concurrency))]);
}
