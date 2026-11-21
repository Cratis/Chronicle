// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_no_failed_partitions_exist : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        var observer = Substitute.For<IObserver>();
        observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([]));
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _error = await Catch.Exception(() =>
        new RetryFailedPartitions("store", "namespace", "unknown-observer", "event-log").Handle(_grainFactory, _storage));

    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<FailedPartitionsRecoveryCouldNotStart>();
    [Fact] void should_identify_the_observer() => _error.Message.ShouldContain("unknown-observer");
}
