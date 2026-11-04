// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_a_reactor_source_event_store_is_configured : Specification
{
    readonly List<(string Store, EventTypeId Id)> _subscriptions = [];
    EventStore _store;

    void Establish()
    {
        var registry = RegisteredEventTypesForSpecifications.Create((typeof(Requested), new("registry-identity", 4)));
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
        var discovered = Substitute.For<IClientArtifactsProvider>();
        discovered.Reactors.Returns([typeof(Reactor), typeof(UnconfiguredReactor)]);
        discovered.Reducers.Returns([]);
        var artifacts = new ExplicitArtifacts().RegisterReactor<Reactor>(ReactorRegistrationOptions.FromEventStore("host"))
            .CreateSnapshot().ApplyTo(discovered);
        var reactors = Substitute.For<IReactors>();
        foreach (var type in artifacts.Reactors)
        {
            var handler = Substitute.For<IReactorHandler>();
            handler.ReactorType.Returns(type);
            var handledEventTypes = ReactorInvoker.GetEventTypesFor(registry, type);
            handler.EventTypes.Returns(handledEventTypes);
            reactors.GetHandlerFor(type).Returns(handler);
        }
        _store = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        Set("_eventStoreName", new EventStoreName("current"));
        Set("_clientArtifactsProvider", artifacts);
        Set("<EventTypes>k__BackingField", registry);
        Set("<Reactors>k__BackingField", reactors);
        Set("<Reducers>k__BackingField", Substitute.For<IReducers>());
        Set("<Projections>k__BackingField", projections);
        Set("<Subscriptions>k__BackingField", subscriptions);
    }

    async Task Because() => await (Task)typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_store, [])!;

    [Fact] void should_subscribe_to_the_configured_store_with_the_handled_id() => _subscriptions.ShouldContain(("host", new EventTypeId("registry-identity")));
    [Fact] void should_not_add_a_subscription_for_the_unconfigured_reactor() => _subscriptions.Count.ShouldEqual(1);

    void Set(string field, object value) => typeof(EventStore).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, value);

    public record Requested;
    public class Reactor : IReactor
    {
        public Task Handle(Requested @event) => Task.CompletedTask;
    }
    public class UnconfiguredReactor : IReactor
    {
        public Task Handle(Requested @event) => Task.CompletedTask;
    }
}
