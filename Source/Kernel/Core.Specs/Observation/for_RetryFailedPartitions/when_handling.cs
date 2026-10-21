// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_handling : Specification
{
    IGrainFactory _grainFactory;
    IObserver _observer;
    RetryFailedPartitionsResult _result;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>(
        [
            new Key("one", Properties.ArrayIndexers.NoIndexers),
            new Key("two", Properties.ArrayIndexers.NoIndexers)
        ]));
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _result = await new RetryFailedPartitions("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory);

    [Fact] void should_tell_how_many_partitions_were_failed() => _result.FailedPartitions.ShouldEqual(2);
    [Fact] void should_ask_the_observer_to_recover_them_in_bounded_batches() => _observer.Received(1).TryRecoverAllFailedPartitions();
}
