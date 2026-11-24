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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookDefinition")]
public record WebhookDefinition(
    [Id(0)] WebhookId Identifier,
    [Id(1)] WebhookOwner Owner,
    [Id(2)] EventSequenceId EventSequenceId,
    [Id(3)] IEnumerable<EventType> EventTypes,
    [Id(4)] WebhookTarget Target,
    [Id(5)] bool IsReplayable = true,
    [Id(6)] bool IsActive = true)
{
    /// <summary>
    /// Gets the policy for delivering event generations.
    /// </summary>
    [Id(7)]
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the signing policy. Existing webhooks remain unsigned until explicitly enabled.
    /// </summary>
    [Id(8)]
    public WebhookSigningMode Signing { get; init; } = WebhookSigningMode.None;
}
