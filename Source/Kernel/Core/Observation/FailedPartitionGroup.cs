// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the current failed partitions of one observer that share a failure kind.
/// </summary>
/// <param name="Id">The identifier of the group, made of the observer and the failure kind.</param>
/// <param name="ObserverId">The identifier of the observer the partitions belong to.</param>
/// <param name="Kind">The kind of the latest attempt on each partition in the group.</param>
/// <param name="PartitionCount">How many partitions are currently failed in the group.</param>
/// <param name="QuarantinedCount">How many of those partitions are quarantined.</param>
/// <param name="FirstObserved">When the earliest failure in the group was observed, or null if no attempt was recorded.</param>
/// <param name="LastObserved">When the most recent failure in the group was observed, or null if no attempt was recorded.</param>
/// <param name="ExamplePartitions">A bounded sample of the partitions in the group, most recently failed first.</param>
[ReadModel]
public record FailedPartitionGroup(
    string Id,
    string ObserverId,
    FailureKind Kind,
    int PartitionCount,
    int QuarantinedCount,
    DateTimeOffset? FirstObserved,
    DateTimeOffset? LastObserved,
    IEnumerable<string> ExamplePartitions)
{
    /// <summary>
    /// Observes the current failed partitions of an event store and namespace, grouped by observer and failure kind.
    /// </summary>
    /// <param name="eventStore">The event store the failed partitions are for.</param>
    /// <param name="namespace">The namespace within the event store the failed partitions are for.</param>
    /// <param name="observerId">Optional observer identifier to narrow the result to a single observer.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the failed partitions.</param>
    /// <returns>An observable subject emitting collections of <see cref="FailedPartitionGroup"/>.</returns>
    public static ISubject<IEnumerable<FailedPartitionGroup>> AllFailedPartitionGroups(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string? observerId,
        IStorage storage)
    {
        var observable = storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).FailedPartitions
            .ObserveAllFor(ToObserverId(observerId))
            .Select(partitions => partitions.ToGroups());

        // Preserve subscription ownership: disconnecting an HTTP observer must stop the SQL polling too.
        return System.Reactive.Subjects.Subject.Create<IEnumerable<FailedPartitionGroup>>(
            System.Reactive.Observer.Create<IEnumerable<FailedPartitionGroup>>(_ => { }),
            observable);
    }

    static ObserverId? ToObserverId(string? observerId) =>
        string.IsNullOrEmpty(observerId) ? null : (ObserverId)observerId;
}
