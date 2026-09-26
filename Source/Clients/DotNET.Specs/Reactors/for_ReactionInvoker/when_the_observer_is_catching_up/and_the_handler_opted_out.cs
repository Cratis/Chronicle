// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_the_observer_is_catching_up;

/// <summary>
/// Catch-up used to be indistinguishable from live, so [Replay] - the only guard on offer - did nothing for
/// the far more common trigger, and an author who added it got justified confidence and no protection (#3901).
/// </summary>
public class and_the_handler_opted_out : Specification
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

    async Task Because()
    {
        var catchUp = EventContext.Empty with { ObservationState = EventObservationState.CatchUp };
        await _invoker.Invoke(new MyEvent(), catchUp);
        await _invoker.Invoke(new MyOtherEvent(), catchUp);
    }

    [Fact] void should_not_notify_anyone() => _reactor.NotificationsSent.ShouldEqual(0);
    [Fact] void should_still_run_the_handler_that_did_not_opt_out() => _reactor.RecordsUpdated.ShouldEqual(1);
}
