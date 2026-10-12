// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_using_explicit_artifacts;

public class and_a_custom_effect_handler_takes_the_delivery : Specification
{
    ReactorScenario<Observer> _scenario;
    IReactorSideEffectHandlers _handlers;
    DeliveryLog _log;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Triggered>(_ => _.ForEventStore("foreign")).RegisterReactor<Observer>("custom-effect-observer");
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(typeof(CustomEffect)).Returns(true);
        _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Task.FromResult(Cratis.Monads.Result.Success<ReactorSideEffectFailure>()));
        _scenario = new(new Defaults(new InMemoryClientArtifactsProvider(), null, options), sideEffectHandlers: _handlers);
        _log = new();
        _scenario.Services.AddSingleton(_log);
    }

    Task Because() => _scenario.Given.ForEventSourceId("partition").Events(new Triggered());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_deliver_with_the_effective_observer_alias() => _log.Delivery.Reactor.Value.ShouldEqual("custom-effect-observer");
    [Fact] void should_use_the_custom_classified_handlers_foreign_sequence() => _log.Delivery.EventSequence.Value.ShouldEqual("inbox-foreign");
    [Fact] void should_dispatch_the_custom_effect_once() => _handlers.Received(1).Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Is<object>(value => value is CustomEffect));

    public record Triggered;
    public record CustomEffect;
    public class DeliveryLog
    {
        public ReactorDelivery Delivery { get; set; }
    }
    public class Observer(DeliveryLog log)
    {
        public CustomEffect Handle(Triggered @event, ReactorDelivery delivery)
        {
            log.Delivery = delivery;
            return new();
        }
    }
}
