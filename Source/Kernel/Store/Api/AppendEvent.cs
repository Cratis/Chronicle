// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Events.Store.Api;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/>.</param>
/// <param name="EventType">The <see cref="EventType"/> to append.</param>
/// <param name="Content">The content of the event represented as <see cref="JsonObject"/>.</param>
/// <returns></returns>
public record AppendEvent(EventSourceId EventSourceId, EventType EventType, JsonObject Content);
