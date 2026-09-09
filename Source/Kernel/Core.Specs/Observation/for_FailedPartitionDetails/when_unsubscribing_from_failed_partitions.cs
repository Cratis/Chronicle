// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.for_FailedPartitionDetails;

public class when_unsubscribing_from_failed_partitions : Specification
{
    Subject<IEnumerable<Concepts.Observation.FailedPartition>> _source;
    IDisposable _subscription;
    bool _wasObserved;

    void Establish()
    {
        _source = new();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").GetNamespace("namespace").FailedPartitions.ObserveAllFor().Returns(_source);
        _subscription = FailedPartitionDetails.AllFailedPartitions("store", "namespace", null, storage).Subscribe(_ => { });
        _wasObserved = _source.HasObservers;
    }

    void Because() => _subscription.Dispose();

    [Fact] void should_have_observed_the_storage() => _wasObserved.ShouldBeTrue();
    [Fact] void should_release_the_storage_subscription() => _source.HasObservers.ShouldBeFalse();

    void Destroy() => _source.Dispose();
}
