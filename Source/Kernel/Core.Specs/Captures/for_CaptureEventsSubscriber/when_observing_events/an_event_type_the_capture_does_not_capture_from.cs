// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.when_observing_events;

public class an_event_type_the_capture_does_not_capture_from : given.a_capture_events_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await Observe(Incoming(1, "SomethingElse", "pending"));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_name_the_event_type() => _result.ExceptionMessages.Any(_ => _.Contains("SomethingElse")).ShouldBeTrue();
}
