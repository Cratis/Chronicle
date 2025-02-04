// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IRetryFailedPartition"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverSubscription">The <see cref="ObserverSubscription"/> for the observer.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
/// <param name="FromSequenceNumber">From <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="EventTypes">The event types to replay.</param>
public record RetryFailedPartitionRequest(
    ObserverKey ObserverKey,
    ObserverSubscription ObserverSubscription,
    Key Key,
    EventSequenceNumber FromSequenceNumber,
    IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverSubscription, Key);
