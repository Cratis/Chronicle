// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Keys;
using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserverPartition"/>.
/// </summary>
/// <param name="ObserverId">The identifier of the observer to replay.</param>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverSubscription">The <see cref="ObserverSubscription"/> for the observer.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
/// <param name="FromSequenceNumber">From <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="ToSequenceNumber">To <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="EventTypes">The event types to replay.</param>
public record ReplayObserverPartitionRequest(
    ObserverId ObserverId,
    ObserverKey ObserverKey,
    ObserverSubscription ObserverSubscription,
    Key Key,
    EventSequenceNumber FromSequenceNumber,
    EventSequenceNumber ToSequenceNumber,
    IEnumerable<EventType> EventTypes);
