// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents the registration of a single client webhook observer.
/// </summary>
/// <param name="Identifier"><see cref="WebhookId"/> of the webhook.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="Target">The <see cref="WebhookTarget"/> target to send the events to.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the webhook is for.</param>
/// <param name="IsReplayable">Whether the webhook supports replay scenarios.</param>
/// <param name="IsActive">Whether the wehbook is active.</param>
public record WebhookDefinition(
    WebhookId Identifier,
    IEnumerable<EventType> EventTypes,
    WebhookTarget Target,
    EventSequenceId EventSequenceId,
    bool IsReplayable,
    bool IsActive)
{
    /// <summary>
    /// Gets the policy for delivering event generations.
    /// </summary>
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;
}
