// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStoreReactorSideEffectHandlerInstances.when_enumerating_handlers;

public class with_two_event_stores : Specification
{
    ServiceProvider _provider;
    IReactorSideEffectHandler[] _firstHandlers;
    IReactorSideEffectHandler[] _secondHandlers;
    bool _firstResult;
    bool _secondResult;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddCratisChronicleClient();
        services.AddSelfBindings();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because()
    {
        var instances = new EventStoreReactorSideEffectHandlerInstances(_provider);
        _firstHandlers = instances.ToArray();
        _secondHandlers = instances.ToArray();

        var firstStore = StoreWhere(knowsEvent: true);
        var secondStore = StoreWhere(knowsEvent: false);
        var context = new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty);
        _firstResult = _firstHandlers.OfType<EventResultHandler>().Single().CanHandle(context, firstStore, new SomeEvent());
        _secondResult = _secondHandlers.OfType<EventResultHandler>().Single().CanHandle(context, secondStore, new SomeEvent());
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_include_each_registry_dependent_handler() =>
        _firstHandlers.Where(handler => handler is EventResultHandler or EventsResultHandler or MixedSideEffectsResultHandler)
            .Select(handler => handler.GetType())
            .ShouldContainOnly([typeof(EventResultHandler), typeof(EventsResultHandler), typeof(MixedSideEffectsResultHandler)]);
    [Fact] void should_create_registry_dependent_handlers_for_each_enumeration() =>
        _firstHandlers.Where(handler => handler is EventResultHandler or EventsResultHandler or MixedSideEffectsResultHandler)
            .Zip(_secondHandlers.Where(handler => handler is EventResultHandler or EventsResultHandler or MixedSideEffectsResultHandler))
            .All(pair => !ReferenceEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_use_the_first_event_stores_registry() => _firstResult.ShouldBeTrue();
    [Fact] void should_use_the_second_event_stores_registry() => _secondResult.ShouldBeFalse();

    static IEventStore StoreWhere(bool knowsEvent)
    {
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.HasFor(typeof(SomeEvent)).Returns(knowsEvent);
        var store = Substitute.For<IEventStore>();
        store.EventTypes.Returns(eventTypes);
        return store;
    }

    record SomeEvent;
}
