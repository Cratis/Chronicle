// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_observing;

public class and_the_database_has_not_changed : given.a_failed_partition_storage
{
    int _receivedCount;
    int _queryCount;
    IDisposable _subscription;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.FailedPartitions.Add(new FailedPartition
        {
            Id = Guid.NewGuid(),
            ObserverId = "the-observer",
            Partition = "the-partition",
            StateJson = string.Empty
        });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        var threeQueriesCompleted = new TaskCompletionSource();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ =>
            {
                _queryCount++;
                if (_queryCount >= 3) threeQueriesCompleted.TrySetResult();
                return new DbContextScope<NamespaceDbContext>(CreateContext(), () => { });
            });

        _subscription = _storage.ObserveAllFor().Subscribe(_ => _receivedCount++);
        await threeQueriesCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_emit_only_the_initial_snapshot() => _receivedCount.ShouldEqual(1);

    void Destroy() => _subscription?.Dispose();
}
