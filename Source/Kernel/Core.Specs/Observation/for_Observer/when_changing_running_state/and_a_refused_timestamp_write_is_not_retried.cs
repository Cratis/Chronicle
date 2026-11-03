// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_refused_timestamp_write_is_not_retried : given.an_observer
{
    async Task Establish()
    {
        await Crash();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _eventStoreNamespaceStorage.Observers.SaveRunningStateChangedAt(Arg.Any<ObserverId>(), Arg.Any<DateTimeOffset?>(), Arg.Any<EventSequenceId>()).Returns(false);
        _eventStoreNamespaceStorage.Observers.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Unsubscribe();
        await _observer.SetHandledStats(1UL);
        await _observer.SetHandledStats(2UL);
        await _observer.SetHandledStats(3UL);
    }

    [Fact] void should_try_the_timestamp_write_only_once() => _eventStoreNamespaceStorage.Observers.Received(1).SaveRunningStateChangedAt(Arg.Any<ObserverId>(), Arg.Any<DateTimeOffset?>(), Arg.Any<EventSequenceId>());
}
