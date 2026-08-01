// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// This is what the method-level placement buys over the class-level one. Marking the class takes every
/// handler out of replay; marking the method takes out only the handler with the side effect that must not
/// repeat, and the reactor's other handlers replay as they always did.
/// </summary>
public class and_only_one_of_two_handlers_is_once_only : Specification
{
    ReactorWithOneOnceOnlyHandler _reactor;
    ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithOneOnceOnlyHandler();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOtherEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithOneOnceOnlyHandler),
            new ActivatedArtifact(_reactor, typeof(ReactorWithOneOnceOnlyHandler), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because()
    {
        var replayed = EventContext.Empty with { ObservationState = EventObservationState.Replay };
        await _invoker.Invoke(new MyEvent(), replayed);
        await _invoker.Invoke(new MyOtherEvent(), replayed);
    }

    [Fact] void should_not_call_the_once_only_handler() => _reactor.OnceOnlyCalls.ShouldEqual(0);
    [Fact] void should_still_call_the_other_handler() => _reactor.OtherCalls.ShouldEqual(1);

    [EventType]
    public record MyOtherEvent();

    class ReactorWithOneOnceOnlyHandler : IReactor
    {
        public int OnceOnlyCalls { get; private set; }

        public int OtherCalls { get; private set; }

        [OnceOnly]
        public void HandleOnceOnly(MyEvent @event) => OnceOnlyCalls++;

        public void HandleOther(MyOtherEvent @event) => OtherCalls++;
    }
}
