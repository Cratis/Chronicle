// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.given;

public class a_reactor_with_a_replay_handler : Specification
{
    protected ReactorWithReplayHandler _reactor;
    protected ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithReplayHandler();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithReplayHandler),
            new ActivatedArtifact(_reactor, typeof(ReactorWithReplayHandler), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    protected static EventContext ContextObservedAs(EventObservationState observationState) =>
        EventContext.Empty with { ObservationState = observationState };

    public class ReactorWithReplayHandler : IReactor
    {
        public int LiveCalls { get; private set; }

        public int ReplayCalls { get; private set; }

        public void OnMyEvent(MyEvent @event) => LiveCalls++;

        [Replay]
        public void HandleDuringReplay(MyEvent @event) => ReplayCalls++;
    }
}
