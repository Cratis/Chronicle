// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the read model for a partition an observer stopped on.
/// </summary>
/// <param name="Id">The unique identifier of the failed partition.</param>
/// <param name="ObserverId">The identifier of the observer the partition belongs to.</param>
/// <param name="Partition">The event source the partition is keyed by.</param>
/// <param name="Attempts">Every attempt made at getting past the failure.</param>
[ReadModel]
public record FailedPartitionDetails(
    Guid Id,
    string ObserverId,
    string Partition,
    IEnumerable<FailedPartitionAttemptDetails> Attempts)
{
    /// <summary>
    /// Gets every failed partition in an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the failed partitions are for.</param>
    /// <param name="namespace">The namespace within the event store the failed partitions are for.</param>
    /// <param name="observerId">Optional observer identifier to narrow the result to a single observer.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the failed partitions.</param>
    /// <returns>A collection of <see cref="FailedPartitionDetails"/>.</returns>
    internal static async Task<IEnumerable<FailedPartitionDetails>> GetFailedPartitions(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string? observerId,
        IStorage storage)
    {
        var failedPartitions = await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).FailedPartitions
            .GetFor(ToObserverId(observerId));

        return failedPartitions.Partitions.ToReadModel();
    }

    /// <summary>
    /// Observes every failed partition in an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the failed partitions are for.</param>
    /// <param name="namespace">The namespace within the event store the failed partitions are for.</param>
    /// <param name="observerId">Optional observer identifier to narrow the result to a single observer.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the failed partitions.</param>
    /// <returns>An observable subject emitting collections of <see cref="FailedPartitionDetails"/>.</returns>
    internal static ISubject<IEnumerable<FailedPartitionDetails>> AllFailedPartitions(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string? observerId,
        IStorage storage)
    {
        var subject = new ReplaySubject<IEnumerable<FailedPartitionDetails>>(1);
        storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).FailedPartitions
            .ObserveAllFor(ToObserverId(observerId))
            .Select(partitions => partitions.ToReadModel())
            .Subscribe(subject.OnNext);
        return subject;
    }

    static Concepts.Observation.ObserverId? ToObserverId(string? observerId) =>
        string.IsNullOrEmpty(observerId) ? null : (Concepts.Observation.ObserverId)observerId;
}
