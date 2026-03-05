// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/>.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes an event to a JSON object.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <returns>Serialized version.</returns>
    JsonObject Serialize(object @event);

    /// <summary>
    /// Deserializes a JSON object to an event of specified type.
    /// </summary>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="json">The JSON object.</param>
    /// <returns>Deserialized event.</returns>
    object Deserialize(Type type, JsonObject json);

    /// <summary>
    /// Deserializes an appended event to its event object.
    /// </summary>
    /// <param name="event">The appended event.</param>
    /// <returns>Deserialized event.</returns>
    object Deserialize(AppendedEvent @event);
}
