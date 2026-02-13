// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for event types being set for a webhook.
/// </summary>
/// <param name="EventTypes">The event types.</param>
[EventType]
public record EventTypesSetForWebhook(IEnumerable<EventType> EventTypes);
