// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents what an alert is about: an observer, and for partition conditions the partition of it.
/// </summary>
/// <remarks>
/// The partition is never null. An alert about the whole observer carries <see cref="AlertPartition.None"/>, because
/// events do not carry nullable properties.
/// </remarks>
/// <param name="EventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the observer belongs to.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer observes.</param>
/// <param name="Partition">The <see cref="AlertPartition"/> the alert is about, or <see cref="AlertPartition.None"/>.</param>
public record AlertTarget(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    AlertPartition Partition)
{
    /// <summary>
    /// Creates the <see cref="AlertTarget"/> for an observer.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="partition">The <see cref="AlertPartition"/> the alert is about.</param>
    /// <returns>The <see cref="AlertTarget"/>.</returns>
    public static AlertTarget For(ObserverKey observer, AlertPartition partition) =>
        new(observer.EventStore, observer.Namespace, observer.ObserverId, observer.EventSequenceId, partition);
}
