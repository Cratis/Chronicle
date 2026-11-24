// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Reactors;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="Identifier"><see cref="ReactorId"/> of the reactor.</param>
/// <param name="Owner">The owner of the reactor.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the reactor is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="IsReplayable">Whether the reactor supports replay scenarios.</param>
/// <param name="Tags">Collection of tags the reactor belongs to.</param>
/// <param name="Filters">The <see cref="ObserverFilters"/> to apply when observing events.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Reactors.ReactorDefinition")]
public record ReactorDefinition(
    [Id(0)] ReactorId Identifier,
    [Id(1)] ReactorOwner Owner,
    [Id(2)] EventSequenceId EventSequenceId,
    [Id(3)] IEnumerable<EventTypeWithKeyExpression> EventTypes,
    [Id(4)] bool IsReplayable = true,
    [Id(5)] IEnumerable<string>? Tags = default,
    [Id(6)] ObserverFilters? Filters = default)
{
    /// <summary>
    /// Gets the event generation delivery policy.
    /// </summary>
    [Id(7)]
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets a value indicating whether the reactor begins at the tail of the event sequence when it is registered for the first time.
    /// </summary>
    [Id(8)]
    public bool StartAtTail { get; init; }

    /// <summary>
    /// Gets the page delivery the client declared, or null when it receives one partition per delivery.
    /// </summary>
    [Id(9)]
    public PageDeliveryDefinition? PageDelivery { get; init; }
}
