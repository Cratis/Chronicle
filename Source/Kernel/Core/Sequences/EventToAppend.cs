// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Content">The JSON payload of the event, as serialized JSON.</param>
/// <param name="Subject">Optional subject that identifies the compliance target for the event.</param>
/// <remarks>
/// Content travels as a JSON string rather than <see cref="System.Text.Json.Nodes.JsonObject"/> because protobuf-net
/// has no serializer for that BCL type - it has no plain reflectable shape, so the gRPC contract generated from this
/// type would produce a field nothing can actually put on the wire.
/// </remarks>
public record EventToAppend(EventType EventType, string Content, string? Subject = null);
