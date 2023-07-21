// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="ObserverId"><see cref="ObserverId"/> of the observer.</param>
/// <param name="Name">The <see cref="ObserverName"/> of the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
public record ClientObserverRegistration(
    ObserverId ObserverId,
    ObserverName Name,
    EventSequenceId EventSequenceId,
    IEnumerable<EventType> EventTypes);
