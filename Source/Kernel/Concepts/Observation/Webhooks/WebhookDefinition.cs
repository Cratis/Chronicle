// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the registration of a single client webhook observer.
/// </summary>
/// <param name="Identifier"><see cref="WebhookId"/> of the webhook.</param>
/// <param name="Owner">The owner of the webhook.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the webhook is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="Target">The <see cref="WebhookTarget"/> target to send the events to.</param>
/// <param name="IsReplayable">Whether the webhook supports replay scenarios.</param>
/// <param name="IsActive">Whether the wehbook is active.</param>
public record WebhookDefinition(
    WebhookId Identifier,
    WebhookOwner Owner,
    EventSequenceId EventSequenceId,
    IEnumerable<EventType> EventTypes,
    WebhookTarget Target,
    bool IsReplayable = true,
    bool IsActive = true);