// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_the_observer_is_catching_up;

public class and_the_events_are_live_instead : Specification
{
    ReactorThatActsOnTheWorld _reactor;
    ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorThatActsOnTheWorld();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOtherEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorThatActsOnTheWorld),
            new ActivatedArtifact(_reactor, typeof(ReactorThatActsOnTheWorld), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because() =>
        await _invoker.Invoke(new MyEvent(), EventContext.Empty with { ObservationState = EventObservationState.Initial });

    [Fact] void should_notify() => _reactor.NotificationsSent.ShouldEqual(1);
}
