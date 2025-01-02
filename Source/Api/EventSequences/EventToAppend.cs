// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Api.EventTypes;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Content">The JSON payload of the event.</param>
public record EventToAppend(EventType EventType, JsonObject Content);
