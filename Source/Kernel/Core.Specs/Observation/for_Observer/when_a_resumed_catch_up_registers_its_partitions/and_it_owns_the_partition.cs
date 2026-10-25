// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_resumed_catch_up_registers_its_partitions;

public class and_it_owns_the_partition : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _jobId = JobId.New();
    bool _isPreparingCatchup;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 40UL };
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(44UL));
    }

    async Task Because()
    {
        var generation = await _observer.ResumingCatchUp(_jobId);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 44UL)]);
    }

    [Fact] void should_hold_the_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"P");
    [Fact] void should_write_the_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_lower_the_preparing_flag() => _isPreparingCatchup.ShouldBeFalse();
    [Fact] void should_not_forward_events_of_the_owned_partition() => _subscriber.DidNotReceive().OnNext((Key)"P", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_forward_events_of_other_partitions() => _subscriber.Received(1).OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
