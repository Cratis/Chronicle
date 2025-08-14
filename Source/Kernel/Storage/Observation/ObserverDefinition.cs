// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the definition of an observer.
/// </summary>
/// <param name="Identifier">The <see cref="ObserverId"/> representing the observer uniquely.</param>
/// <param name="EventTypes">The list of <see cref="EventTypeId"/> representing the event types the observer is interested in.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> representing the current event sequence the observer is on.</param>
/// <param name="Type">The <see cref="ObserverType"/> representing the type of the observer.</param>
/// <param name="Owner">The <see cref="ObserverOwner"/> representing the owner of the observer.</param>
/// <param name="IsReplayable">A boolean indicating whether the observer is replayable.</param>
public record ObserverDefinition(
    ObserverId Identifier,
    IEnumerable<EventType> EventTypes,
    EventSequenceId EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    bool IsReplayable)
{
    /// <summary>
    /// Represents an empty observer definition.
    /// </summary>
    public static readonly ObserverDefinition Empty = new(ObserverId.Unspecified, [], EventSequenceId.Unspecified, ObserverType.Unknown, ObserverOwner.None, false);
}
