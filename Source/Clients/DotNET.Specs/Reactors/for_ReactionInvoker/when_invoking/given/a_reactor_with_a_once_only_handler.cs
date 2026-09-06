// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.given;

public class a_reactor_with_a_once_only_handler : Specification
{
    protected ReactorWithOnceOnlyHandler _reactor;
    protected ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithOnceOnlyHandler();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithOnceOnlyHandler),
            new ActivatedArtifact(_reactor, typeof(ReactorWithOnceOnlyHandler), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    protected static EventContext ContextObservedAs(EventObservationState observationState) =>
        EventContext.Empty with { ObservationState = observationState };

    public class ReactorWithOnceOnlyHandler : IReactor
    {
        public int Calls { get; private set; }

        [OnceOnly]
        public void OnMyEvent(MyEvent @event) => Calls++;
    }
}
