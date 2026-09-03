// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;

using FailedPartitionsState = Cratis.Chronicle.Concepts.Observation.FailedPartitions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_observing;

public class and_a_partition_is_resolved : given.a_failed_partition_storage
{
    static readonly ObserverId _observerId = "the-observer";
    static readonly Key _partition = new("the-partition", ArrayIndexers.NoIndexers);

    FailedPartitionsState _state;
    IEnumerable<Concepts.Observation.FailedPartition> _received;
    IDisposable _subscription;

    async Task Establish()
    {
        _state = new FailedPartitionsState();
        var failure = _state.RegisterAttempt(_partition, EventSequenceNumber.First, ["failed"], string.Empty);
        failure.ObserverId = _observerId;
        await _storage.Save(_observerId, _state);
    }

    async Task Because()
    {
        var initial = new TaskCompletionSource();
        var completion = new TaskCompletionSource<IEnumerable<Concepts.Observation.FailedPartition>>();
        _subscription = _storage.ObserveAllFor().Subscribe(partitions =>
        {
            if (partitions.Any()) initial.TrySetResult();
            if (initial.Task.IsCompleted && !partitions.Any()) completion.TrySetResult(partitions);
        });

        await initial.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _state.Remove(_partition);
        await _storage.Save(_observerId, _state);

        _received = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_publish_the_removal() => _received.ShouldBeEmpty();

    void Destroy() => _subscription?.Dispose();
}
