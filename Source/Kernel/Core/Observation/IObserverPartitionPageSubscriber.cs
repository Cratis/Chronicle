// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a subscriber that receives a page of events spanning several partitions in one delivery and settles each
/// partition on its own. This is the shape a client observer that declared page delivery receives.
/// </summary>
[Version(1)]
public interface IObserverPartitionPageSubscriber : IObserverSubscriber
{
    /// <summary>
    /// Handle a page of events spanning partitions.
    /// </summary>
    /// <param name="batches">The partition batches of the page. Each holds one partition's events in sequence order.</param>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/> for the delivery.</param>
    /// <returns>An <see cref="ObserverPageResult"/> with one result per partition.</returns>
    [ResponseTimeout(ObserverSubscriberResponseTimeout.Value)]
    Task<ObserverPageResult> OnNextPartitions(IReadOnlyList<ObserverPartitionBatch> batches, ObserverSubscriberContext context);
}
