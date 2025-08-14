// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the definition of an observer.
/// </summary>
/// <param name="Identifier"><see cref="ObserverId"/> of the observer.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> the observer is interested in.</param>
/// <param name="EventSequenceId"><see cref="EventSequenceId"/> the observer is associated with.</param>
/// <param name="Type"><see cref="ObserverType"/> of the observer.</param>
/// <param name="Owner"><see cref="ObserverOwner"/> of the observer.</param>
/// <param name="IsReplayable">Whether the observer supports replay scenarios.</param>
public record ObserverDefinition(
    ObserverId Identifier,
    IEnumerable<EventType> EventTypes,
    EventSequenceId EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    bool IsReplayable);
