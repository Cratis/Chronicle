// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_resolving_the_handler_method;

/// <summary>
/// The richest signature wins: the handler that asked for the most context is the more specific one, and it is
/// the one the author meant to run.
/// </summary>
public class and_two_public_handlers_differ_in_signature : Specification
{
    ReactorWithTwoHandlerSignatures _reactor;
    ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithTwoHandlerSignatures();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithTwoHandlerSignatures),
            new ActivatedArtifact(_reactor, typeof(ReactorWithTwoHandlerSignatures), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because() => await _invoker.Invoke(new MyEvent(), EventContext.Empty);

    [Fact] void should_call_the_handler_taking_the_event_context() => _reactor.EventAndContextCalls.ShouldEqual(1);
    [Fact] void should_not_call_the_handler_taking_only_the_event() => _reactor.EventOnlyCalls.ShouldEqual(0);

    class ReactorWithTwoHandlerSignatures : IReactor
    {
        public int EventOnlyCalls { get; private set; }

        public int EventAndContextCalls { get; private set; }

        public void OnWithContext(MyEvent @event, EventContext context) => EventAndContextCalls++;

        public void On(MyEvent @event) => EventOnlyCalls++;
    }
}
