// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
using Cratis.Chronicle.Schemas;
using KernelConcepts::Cratis.Chronicle.Concepts;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelCore::Cratis.Chronicle.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a minimal in-memory implementation of the kernel <see cref="IEventTypes"/> for in-process testing.
/// </summary>
/// <remarks>
/// Only the <see cref="GetJsonSchema"/> method is used by the kernel <see cref="KernelCore::Cratis.Chronicle.EventSequences.EventSerializer"/>
/// during event deserialization. Since test scenarios only append events (serialization only), this implementation
/// returns an empty schema for all types, causing the <see cref="Cratis.Chronicle.Json.ExpandoObjectConverter"/>
/// to fall back to generic conversion.
/// </remarks>
internal sealed class InMemoryKernelEventTypes : IEventTypes
{
    /// <inheritdoc/>
    public JsonSchema GetJsonSchema(Type eventType) => new();

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) =>
        throw new NotSupportedException("GetClrTypeFor is not supported in in-process testing; deserialization is not needed for event append scenarios.");

    /// <inheritdoc/>
    public Task DiscoverAndRegister(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore) => Task.CompletedTask;
}
