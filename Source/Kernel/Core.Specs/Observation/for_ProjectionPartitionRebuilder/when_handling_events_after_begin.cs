// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder;

public class when_handling_events_after_begin : given.a_rebuilder
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        await _rebuilder.Begin(_observerKey, _partition);
        _result = await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2)], new(null));
    }

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_last_event_handled() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_handle_into_an_isolated_sink() => _isolatedSink.ShouldNotBeSame(_liveSink);
    [Fact] void should_not_touch_the_live_read_model() => _liveSink.ReceivedCalls().ShouldBeEmpty();
}
