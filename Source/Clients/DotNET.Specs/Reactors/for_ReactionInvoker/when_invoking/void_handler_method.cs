// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class void_handler_method : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        var reactor = new ReactorWithVoidHandlerMethodForEventType();

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithVoidHandlerMethodForEventType),
            new ActivatedArtifact(reactor, typeof(ReactorWithVoidHandlerMethodForEventType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), EventContext.Empty);

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_call_before_invoke() => _middlewares.Received(1).BeforeInvoke(Arg.Any<EventContext>(), Arg.Any<object>());
    [Fact] void should_call_after_invoke() => _middlewares.Received(1).AfterInvoke(Arg.Any<EventContext>(), Arg.Any<object>());

    class ReactorWithVoidHandlerMethodForEventType : IReactor
    {
        public void Handle(MyEvent @event)
        {
        }
    }
}