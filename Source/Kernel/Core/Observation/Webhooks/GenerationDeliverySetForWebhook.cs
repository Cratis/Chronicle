// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Records a webhook's delivery policy together with the event types it pins.
/// </summary>
/// <param name="GenerationDelivery">The generation delivery policy.</param>
/// <param name="EventTypes">The subscribed event types, including their generations.</param>
[EventType, AllEventStores]
public record GenerationDeliverySetForWebhook(EventGenerationDelivery GenerationDelivery, IEnumerable<EventType> EventTypes);
