// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents a MongoDB version of observer definition.
/// </summary>
/// <param name="Id">The <see cref="ObserverId"/> representing the observer uniquely.</param>
/// <param name="EventTypes">The list of <see cref="EventTypeId"/> representing the event types the observer is interested in.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> representing the current event sequence the observer is on.</param>
/// <param name="Type">The <see cref="ObserverType"/> representing the type of the observer.</param>
/// <param name="Owner">The <see cref="ObserverOwner"/> representing the owner of the observer.</param>
/// <param name="IsReplayable">A boolean indicating whether the observer is replayable.</param>
public record ObserverDefinition(
    ObserverId Id,
    IEnumerable<EventTypeId> EventTypes,
    EventSequenceId EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    bool IsReplayable);
