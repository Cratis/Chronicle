// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;
using FailedPartitionsState = Cratis.Chronicle.Concepts.Observation.FailedPartitions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_observing;

public class and_only_a_deferral_deadline_changes : given.a_failed_partition_storage
{
    static readonly ObserverId _observerId = "the-observer";
    static readonly Key _partition = new("the-partition", ArrayIndexers.NoIndexers);
    DateTimeOffset _deadline;
    Concepts.Observation.FailedPartition _received;
    IDisposable _subscription;

    async Task Because()
    {
        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        var state = new FailedPartitionsState();
        var failure = state.Defer(_partition, new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, since, since.AddMinutes(1), EventSequenceNumber.First));
        failure.ObserverId = _observerId;
        await _storage.Save(_observerId, state);

        var initial = new TaskCompletionSource();
        var changed = new TaskCompletionSource<Concepts.Observation.FailedPartition>();
        _deadline = since.AddMinutes(3);
        _subscription = _storage.ObserveAllFor().Subscribe(partitions =>
        {
            var partition = partitions.SingleOrDefault();
            if (partition?.Deferral?.Until == since.AddMinutes(1)) initial.TrySetResult();
            if (partition?.Deferral?.Until == _deadline) changed.TrySetResult(partition);
        });
        await initial.Task.WaitAsync(TimeSpan.FromSeconds(30));
        failure.Deferral = failure.Deferral with { Until = _deadline };
        await _storage.Save(_observerId, state);
        _received = await changed.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_publish_the_changed_deadline() => _received.Deferral.Until.ShouldEqual(_deadline);

    void Destroy() => _subscription?.Dispose();
}
