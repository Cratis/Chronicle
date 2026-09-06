// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// Every reactor written before replay handlers existed relies on its handler running during a replay, so an
/// event type with no replay handler has to keep behaving exactly as it did.
/// </summary>
public class and_only_a_live_handler_exists_during_replay : Specification
{
    ReactorWithoutReplayHandler _reactor;
    ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithoutReplayHandler();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithoutReplayHandler),
            new ActivatedArtifact(_reactor, typeof(ReactorWithoutReplayHandler), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because() => await _invoker.Invoke(new MyEvent(), EventContext.Empty with { ObservationState = EventObservationState.Replay });

    [Fact] void should_call_the_live_handler() => _reactor.LiveCalls.ShouldEqual(1);

    class ReactorWithoutReplayHandler : IReactor
    {
        public int LiveCalls { get; private set; }

        public void OnMyEvent(MyEvent @event) => LiveCalls++;
    }
}
