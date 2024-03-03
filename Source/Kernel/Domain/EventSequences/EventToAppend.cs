// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Content">The JSON payload of the event.</param>
/// <param name="ValidFrom">Optional date and time for when the compensation is valid from. </param>
public record EventToAppend(EventType EventType, JsonObject Content, DateTimeOffset? ValidFrom = default);
