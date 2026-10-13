// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_events_are_registered_without_markers : Specification
{
    readonly List<(string Store, EventTypeId Id)> _subscriptions = [];
    IEventTypes _registry;

    void Establish() => _registry = RegisteredEventTypesForSpecifications.Create((typeof(Foreign), new("registry-identity", 4)));

    async Task Because()
    {
        await Register("current", typeof(Reactor));
        await Register("other", typeof(Reactor));
        await Register("current", typeof(ExplicitSequenceReactor));
        await Register("current", typeof(OverriddenStoreReactor));
    }

    [Fact] void should_subscribe_only_the_foreign_and_overridden_stores() => _subscriptions.Count.ShouldEqual(2);
    [Fact] void should_use_the_registry_id_for_the_inferred_store() => _subscriptions.ShouldContain(("other", new EventTypeId("registry-identity")));
    [Fact] void should_honor_the_observer_store_override() => _subscriptions.ShouldContain(("override", new EventTypeId("registry-identity")));

    async Task Register(string currentStore, Type reactorType)
    {
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
                builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder).AndDoes(configuration =>
                    _subscriptions.Add((call.Arg<string>(), configuration.Arg<EventTypeId>())));
                call.Arg<Action<IEventStoreSubscriptionBuilder>>()(builder);
                return Task.CompletedTask;
            });
        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([]);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reactors.Returns([reactorType]);
        artifacts.Reducers.Returns([]);
        var reactors = Substitute.For<IReactors>();
        var handler = Substitute.For<IReactorHandler>();
        handler.ReactorType.Returns(reactorType);
        var handledEventTypes = ReactorInvoker.GetEventTypesFor(_registry, reactorType);
        handler.EventTypes.Returns(handledEventTypes);
        reactors.GetHandlerFor(reactorType).Returns(handler);
        var store = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        Set(store, "_eventStoreName", new EventStoreName(currentStore));
        Set(store, "_clientArtifactsProvider", artifacts);
        Set(store, "<EventTypes>k__BackingField", _registry);
        Set(store, "<Reactors>k__BackingField", reactors);
        Set(store, "<Reducers>k__BackingField", Substitute.For<IReducers>());
        Set(store, "<Projections>k__BackingField", projections);
        Set(store, "<Subscriptions>k__BackingField", subscriptions);
        await (Task)typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, [])!;
    }

    static void Set(EventStore store, string field, object value) =>
        typeof(EventStore).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(store, value);

    [EventStore("other")]
    public record Foreign;
    public class Reactor : IReactor
    {
        public Task Handle(Foreign @event) => Task.CompletedTask;
    }
    [EventSequence("custom")]
    public class ExplicitSequenceReactor : IReactor
    {
        public Task Handle(Foreign @event) => Task.CompletedTask;
    }
    [EventStore("override")]
    public class OverriddenStoreReactor : IReactor
    {
        public Task Handle(Foreign @event) => Task.CompletedTask;
    }
}
