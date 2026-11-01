// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_no_partition_replay_is_completing : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish() => _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(ObserverSubscriberResult.Ok(42UL));

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_persist_a_null_completion_field() => _stateStorage.State.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_make_only_the_existing_two_writes() => _storageStats.Writes.ShouldEqual(2);
}
