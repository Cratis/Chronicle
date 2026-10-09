// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Builds the JSON the $context and $eventSourceId expressions of an events capture resolve against.
/// </summary>
public static class CapturedEventContext
{
    /// <summary>
    /// Create the JSON representation of an event context.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the incoming event.</param>
    /// <returns>A <see cref="JsonObject"/> with camel cased context properties.</returns>
    public static JsonObject ToJson(EventContext context) => new()
    {
        ["eventType"] = context.EventType.Id.Value,
        ["eventSourceType"] = context.EventSourceType.Value,
        ["eventSourceId"] = context.EventSourceId.Value,
        ["eventStreamType"] = context.EventStreamType.Value,
        ["eventStreamId"] = context.EventStreamId.Value,
        ["sequenceNumber"] = context.SequenceNumber.Value,
        ["occurred"] = context.Occurred.ToString("O"),
        ["correlationId"] = context.CorrelationId.ToString(),
        ["subject"] = context.Subject?.Value
    };
}
