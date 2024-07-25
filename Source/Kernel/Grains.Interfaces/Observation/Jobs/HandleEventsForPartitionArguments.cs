// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the arguments passed along to a job step representing a specific key on an observer.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> with extended details about the observer.</param>
/// <param name="ObserverSubscription">The <see cref="ObserverSubscription"/> for the observer.</param>
/// <param name="Partition">The partition in the form a <see cref="Key"/>.</param>
/// <param name="StartEventSequenceNumber">The event sequence number the job step should start from.</param>
/// <param name="EventObservationState">The event observation state to set for the events.</param>
/// <param name="EventTypes">The event types that are to replay.</param>
public record HandleEventsForPartitionArguments(
    ObserverKey ObserverKey,
    ObserverSubscription ObserverSubscription,
    Key Partition,
    EventSequenceNumber StartEventSequenceNumber,
    EventObservationState EventObservationState,
    IEnumerable<EventType> EventTypes);
