// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the event for a webhook that has been added.
/// </summary>
/// <param name="Identifier">The webhook identifier.</param>
/// <param name="Owner">The owner of the webhook.</param>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="EventTypes">The event types the webhook observes.</param>
/// <param name="Target">The webhook target.</param>
/// <param name="IsReplayable">Whether the webhook is replayable.</param>
/// <param name="IsActive">Whether the webhook is active.</param>
[EventType]
public record WebhookAdded(
    WebhookId Identifier,
    WebhookOwner Owner,
    EventSequenceId EventSequenceId,
    IEnumerable<EventType> EventTypes,
    WebhookTarget Target,
    bool IsReplayable,
    bool IsActive);
