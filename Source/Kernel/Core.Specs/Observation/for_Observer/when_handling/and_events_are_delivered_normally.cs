// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_events_are_delivered_normally : given.an_observer_with_skip_detection
{
    void Establish() =>
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(0UL));

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 0UL)]);
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 1UL)]);
    }

    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
    [Fact] void should_not_warn() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
