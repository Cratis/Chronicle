// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents one probe delivery of a selected event to a quarantined webhook.
/// </summary>
/// <param name="Control">The probe state that authorized the delivery.</param>
/// <param name="Failure">The failed partition the event is taken from.</param>
/// <param name="Subscriber">The partition-keyed webhook subscriber.</param>
/// <param name="Selected">The selected event, before release.</param>
/// <param name="Released">The released event batch sent to the receiver.</param>
internal record WebhookProbeDelivery(
    WebhookProbeState Control,
    FailedPartition Failure,
    IWebhookObserverSubscriber Subscriber,
    AppendedEvent Selected,
    AppendedEvent[] Released);
