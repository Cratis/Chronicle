// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.for_RetryPartition;

public class when_the_partition_is_not_failed : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        var observer = Substitute.For<IObserver>();
        observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(PartitionRecoveryOutcome.PartitionNotFound);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _error = await Catch.Exception(() =>
        new RetryPartition("store", "namespace", "observer", "event-log", "healthy-partition").Handle(_grainFactory, _storage));

    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<FailedPartitionRecoveryCouldNotStart>();
    [Fact] void should_identify_the_partition() => _error.Message.ShouldContain("healthy-partition");
}
