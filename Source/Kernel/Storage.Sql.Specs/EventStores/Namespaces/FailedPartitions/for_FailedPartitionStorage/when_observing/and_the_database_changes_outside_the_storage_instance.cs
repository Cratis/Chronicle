// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_observing;

/// <summary>
/// Proves the observable watches shared SQL state rather than relying on a process-local notification
/// from <see cref="FailedPartitionStorage.Save"/>. Another silo writes through a different storage
/// instance but shares the database, so polling the database is the cluster-visible contract.
/// </summary>
public class and_the_database_changes_outside_the_storage_instance : given.a_failed_partition_storage
{
    static readonly Guid _failureId = Guid.NewGuid();
    IEnumerable<Concepts.Observation.FailedPartition> _received;
    IDisposable _subscription;

    async Task Because()
    {
        var completion = new TaskCompletionSource<IEnumerable<Concepts.Observation.FailedPartition>>();
        _subscription = _storage.ObserveAllFor().Subscribe(partitions =>
        {
            if (partitions.Any()) completion.TrySetResult(partitions);
        });

        await using var context = CreateContext();
        context.FailedPartitions.Add(new FailedPartition
        {
            Id = _failureId,
            ObserverId = "another-silo-observer",
            Partition = "the-partition",
            StateJson = string.Empty
        });
        await context.SaveChangesAsync();

        _received = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_publish_the_external_change() => _received.Single().Id.ShouldEqual((FailedPartitionId)_failureId);

    void Destroy() => _subscription?.Dispose();
}
