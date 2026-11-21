// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_no_partition_can_start : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    IObserver _observer;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([(Key)"one", (Key)"two"]));
        _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(PartitionRecoveryOutcome.ObserverQuarantined);
    }

    async Task Because() => _error = await Catch.Exception(() =>
        new RetryFailedPartitions("store", "namespace", "observer", "event-log").Handle(_grainFactory, _storage));

    [Fact] void should_attempt_every_partition() => _observer.Received(2).TryStartRecoverJobForFailedPartition(Arg.Any<Key>());
    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<FailedPartitionsRecoveryCouldNotStart>();
}
