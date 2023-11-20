// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents the payload for an event when used in applying to an immediate projection.
/// </summary>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Content">The JSON payload of the event.</param>
public record EventToApply(EventType EventType, JsonObject Content);
