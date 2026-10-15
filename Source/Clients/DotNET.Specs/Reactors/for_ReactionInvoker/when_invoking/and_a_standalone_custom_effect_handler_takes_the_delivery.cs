// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class and_a_standalone_custom_effect_handler_takes_the_delivery : Specification
{
    ReactorInvoker _invoker;
    StandaloneCustomEffectReactor _reactor;
    IReactorSideEffectHandlers _handlers;
    IEventStore _store;
    EventContext _context;
    ReactorInvocationResult _result;
    int _classifications;

    void Establish()
    {
        var registry = new EventTypesForSpecifications([typeof(Triggered)]);
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(Arg.Any<Type>()).Returns(call =>
        {
            _classifications++;
            return call.Arg<Type>() == typeof(CustomEffect);
        });
        _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Task.FromResult(Cratis.Monads.Result.Success<ReactorSideEffectFailure>()));
        _store = Substitute.For<IEventStore>();
        _store.Name.Returns((EventStoreName)"current");
        _store.EventTypes.Returns(registry);
        _reactor = new();
        _invoker = new(
            registry,
            Substitute.For<IReactorMiddlewares>(),
            typeof(StandaloneCustomEffectReactor),
            new ActivatedArtifact(_reactor, typeof(StandaloneCustomEffectReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            _handlers,
            _store);
        _context = EventContext.EmptyWithEventSourceId("partition") with { EventStore = "current" };
    }

    async Task Because() => _result = await _invoker.Invoke(new Triggered(), _context);

    [Fact] void should_invoke_the_standalone_handler_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_supply_delivery_from_the_already_accepted_dispatch_set() => _reactor.Delivery.ShouldEqual(ReactorDelivery.For(typeof(StandaloneCustomEffectReactor).GetReactorId(), "inbox-foreign", _context));
    [Fact] void should_not_reclassify_the_handler_during_argument_resolution() => _classifications.ShouldEqual(1);
    [Fact] void should_dispatch_the_custom_effect_once() => _handlers.Received(1).Handle(Arg.Any<ReactorContext>(), _store, Arg.Is<object>(value => value is CustomEffect));

    [EventType, EventStore("foreign")]
    public record Triggered;
    public record CustomEffect;
#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class StandaloneCustomEffectReactor : IReactor
    {
        public ReactorDelivery Delivery { get; private set; }
        public CustomEffect Handle(Triggered @event, ReactorDelivery delivery)
        {
            Delivery = delivery;
            return new();
        }
    }
#pragma warning restore ARC0002
}
