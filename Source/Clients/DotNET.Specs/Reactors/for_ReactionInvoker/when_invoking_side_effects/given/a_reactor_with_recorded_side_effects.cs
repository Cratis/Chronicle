// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects.given;

public class a_reactor_with_recorded_side_effects : Specification
{
    protected readonly List<string> _calls = [];
    protected IReactorSideEffectHandlers _handlers;
    protected IReactorMiddlewares _middlewares;
    protected IEventStore _store;
    protected EventContext _context;
    protected ReactorInvocationResult _result;
    protected object _handledValue;
    EventTypesForSpecifications _registry;

    void Establish()
    {
        _registry = new EventTypesForSpecifications([typeof(Triggered)]);
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(Effect));
        _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(call =>
        {
            _calls.Add("side-effect");
            _handledValue = call.Arg<object>();
            return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
        });
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _middlewares.BeforeInvoke(Arg.Any<ReactorId>(), Arg.Any<EventContext>(), Arg.Any<object>()).Returns(_ =>
        {
            _calls.Add("before");
            return Task.CompletedTask;
        });
        _middlewares.AfterInvoke(Arg.Any<ReactorId>(), Arg.Any<EventContext>(), Arg.Any<object>()).Returns(_ =>
        {
            _calls.Add("after");
            return Task.CompletedTask;
        });
        _store = Substitute.For<IEventStore>();
        _store.Name.Returns((EventStoreName)"current");
        _store.EventTypes.Returns(_registry);
        _context = EventContext.EmptyWithEventSourceId("partition") with { EventStore = "current" };
    }

    protected Task<ReactorInvocationResult> Invoke<TReactor>(TReactor reactor)
        where TReactor : class =>
        new ReactorInvoker(
            _registry,
            _middlewares,
            typeof(TReactor),
            new ActivatedArtifact(reactor, typeof(TReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            _handlers,
            _store).Invoke(new Triggered(), _context);

    [EventType]
    public record Triggered;

    public record Effect;
}
