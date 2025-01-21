// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a request to an observer job.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to catch up.</param>
/// <param name="ObserverSubscription">The <see cref="ObserverSubscription"/> for the observer.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
public abstract record ObserverPartitionedJobRequest(ObserverKey ObserverKey, ObserverSubscription ObserverSubscription, Key Key) : IObserverJobRequest
{
    /// <summary>
    /// Creates an <see cref="ObserverSubscriberKey"/> from the request.
    /// </summary>
    /// <returns>The <see cref="ObserverSubscriberKey"/>.</returns>
    public ObserverSubscriberKey ToObserverSubscriberKey() => new(
                    ObserverKey.ObserverId,
                    ObserverKey.EventStore,
                    ObserverKey.Namespace,
                    ObserverKey.EventSequenceId,
                    Key?.ToString() ?? EventSourceId.Unspecified,
                    ObserverSubscription.SiloAddress.ToParsableString());
}
