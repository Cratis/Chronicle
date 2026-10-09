// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class and_a_namespace_fails : given.a_capture_events_subscriptions
{
    Exception _exception;

    void Establish()
    {
        _namespaces = ["first", "second", "third"];
        var broken = Substitute.For<IObserver>();
        AnySubscribe(broken).Returns<Task<IEnumerable<Cratis.Chronicle.Concepts.Events.EventType>>>(_ => throw new TimeoutException());
        _observersByNamespace["second"] = broken;
    }

    async Task Because() => _exception = await Catch.Exception(() => _subscriptions.Subscribe(_eventStore, _definition));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_unsubscribe_the_namespace_already_subscribed() => _observersByNamespace["first"].Received(1).Unsubscribe();
    [Fact] void should_not_attempt_the_later_namespace() => _observersByNamespace.ContainsKey("third").ShouldBeFalse();
}
