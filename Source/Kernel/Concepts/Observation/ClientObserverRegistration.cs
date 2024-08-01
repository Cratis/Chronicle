// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="ObserverId"><see cref="ObserverId"/> of the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
public record ClientObserverRegistration(
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    IEnumerable<EventType> EventTypes);
