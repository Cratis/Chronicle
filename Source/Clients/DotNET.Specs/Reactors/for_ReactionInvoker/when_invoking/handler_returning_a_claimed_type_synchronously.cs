// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_returning_a_claimed_type_synchronously : Specification
{
    ReactorInvocationResult _result;
    IReactorSideEffectHandlers _handlers;
    ReactorInvoker _invoker;

    void Establish()
    {
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(typeof(Claimed)).Returns(true);
        _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Task.FromResult(Cratis.Monads.Result.Success<ReactorSideEffectFailure>()));
        var reactor = new ClaimedReactor();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ClaimedReactor),
            new ActivatedArtifact(reactor, typeof(ClaimedReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            _handlers,
            Substitute.For<IEventStore>());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_dispatch_the_return_value_once() => _handlers.Received(1).Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Is<object>(value => value is Claimed));

    class ClaimedReactor : IReactor
    {
        public Claimed Handle(MyEvent @event) => new();
    }

    record Claimed;
}
