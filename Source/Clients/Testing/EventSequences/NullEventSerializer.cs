// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Text.Json;
using System.Text.Json.Nodes;
using KernelCore::Cratis.Chronicle.EventSequences;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a minimal implementation of <see cref="IEventSerializer"/> for in-process testing.
/// </summary>
/// <remarks>
/// Serializes events using <see cref="JsonSerializer"/> with the default Chronicle JSON options.
/// Deserialization is not needed for append-only test scenarios and throws <see cref="NotSupportedException"/>.
/// </remarks>
internal sealed class NullEventSerializer : IEventSerializer
{
    static readonly JsonSerializerOptions _options = global::Cratis.Json.Globals.JsonSerializerOptions ?? new JsonSerializerOptions();

    /// <inheritdoc/>
    public JsonObject Serialize(object @event) =>
        (JsonSerializer.SerializeToNode(@event, _options) as JsonObject)!;

    /// <inheritdoc/>
    public object Deserialize(Type type, JsonObject json) =>
        throw new NotSupportedException("Deserialization is not supported in test event sequences.");

    /// <inheritdoc/>
    public object Deserialize(AppendedEvent @event) =>
        throw new NotSupportedException("Deserialization is not supported in test event sequences.");
}
