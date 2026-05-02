// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents a failed partition.
/// </summary>
/// <param name="Id">The unique identifier of the failed partition.</param>
/// <param name="ObserverId">The observer id.</param>
/// <param name="Partition">The partition that is failed.</param>
/// <param name="Attempts">The attempts for the failed partition.</param>
[ReadModel]
public record FailedPartition(Guid Id, string ObserverId, string Partition, IEnumerable<FailedPartitionAttempt> Attempts)
{
    /// <summary>
    /// Gets all failed partitions for an event store and namespace.
    /// </summary>
    /// <param name="failedPartitions"><see cref="IFailedPartitions"/> for working with failed partitions.</param>
    /// <param name="eventStore">Event store the failed partitions are for.</param>
    /// <param name="namespace">Namespace the failed partitions are for.</param>
    /// <param name="observerId">Optional observer id to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartition"/>.</returns>
    public static ISubject<IEnumerable<FailedPartition>> AllFailedPartitions(IFailedPartitions failedPartitions, string eventStore, string @namespace, string? observerId) =>
        failedPartitions.InvokeAndWrapWithTransformSubject(
            token => failedPartitions.ObserveFailedPartitions(new() { EventStore = eventStore, Namespace = @namespace, ObserverId = observerId }, token),
            failedPartitions => failedPartitions.ToApi());
}
