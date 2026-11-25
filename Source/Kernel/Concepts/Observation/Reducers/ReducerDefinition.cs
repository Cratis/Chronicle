// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Observation.Reducers;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="Identifier"><see cref="ReducerId"/> of the reducer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="ReadModel">The read model to use.</param>
/// <param name="IsActive">Whether or not the reducer is an actively observing reducer.</param>
/// <param name="Tags">Collection of tags the reducer belongs to.</param>
/// <param name="Filters">The <see cref="ObserverFilters"/> to apply when observing events.</param>
/// <param name="Hash">Fingerprint of the reducer implementation.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Reducers.ReducerDefinition")]
public record ReducerDefinition(
    [Id(0)] ReducerId Identifier,
    [Id(1)] EventSequenceId EventSequenceId,
    [Id(2)] IEnumerable<EventTypeWithKeyExpression> EventTypes,
    [Id(3)] ReadModelIdentifier ReadModel,
    [Id(4)] bool IsActive,
    [Id(5)] IEnumerable<string> Tags,
    [Id(6)] ObserverFilters? Filters = default,
    [Id(7)] string Hash = "")
{
    /// <summary>
    /// Gets the event generation delivery policy.
    /// </summary>
    [Id(8)]
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the page delivery the client declared, or null when it receives one partition per delivery.
    /// </summary>
    [Id(9)]
    public PageDeliveryDefinition? PageDelivery { get; init; }
}
