// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_a_discovered_reactor_has_been_unregistered : an_explicit_store
{
    IEventStoreSubscriptions _subscriptions;
    Exception? _failure;
    readonly List<EventTypeId> _subscribedIds = [];

    async Task Establish()
    {
        _options.ExplicitArtifacts
            .RegisterEventType<RemovedEvent>(_ => _.WithId("removed-event").ForEventStore("removed-source"))
            .RegisterEventType<RetainedEvent>(_ => _.WithId("retained-event").ForEventStore("retained-source"))
            .RegisterReactor<RemovedReactor>()
            .RegisterReactor<RetainedReactor>();
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();
        _subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
                builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder)
                    .AndDoes(configuration => _subscribedIds.Add(configuration.Arg<EventTypeId>()));
                call.Arg<Action<IEventStoreSubscriptionBuilder>>()(builder);
                return Task.CompletedTask;
            });
        _store = CreateStore();
        typeof(EventStore).GetField("<Subscriptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, _subscriptions);
        await _store.DiscoverAll();
        _store.Reactors.Unregister(_store.Reactors.GetHandlerFor(typeof(RemovedReactor)).Id);
    }

    async Task Because()
    {
        var collectSubscriptions = typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<Task>>(_store);
        _failure = await Catch.Exception(collectSubscriptions);
    }

    [Fact] void should_collect_subscriptions_successfully() => _failure.ShouldBeNull();
    [Fact] void should_not_subscribe_for_the_removed_reactor() => _subscriptions.DidNotReceive().Subscribe(Arg.Any<EventStoreSubscriptionId>(), "removed-source", Arg.Any<Action<IEventStoreSubscriptionBuilder>>());
    [Fact] void should_subscribe_for_the_retained_reactor() => _subscriptions.Received(1).Subscribe(Arg.Any<EventStoreSubscriptionId>(), "retained-source", Arg.Any<Action<IEventStoreSubscriptionBuilder>>());
    [Fact] void should_use_only_the_retained_dispatch_event_identity() => _subscribedIds.ShouldContainOnly(new EventTypeId("retained-event"));

    public record RemovedEvent;
    public record RetainedEvent;
    public class RemovedReactor
    {
        public Task Handle(RemovedEvent @event) => Task.CompletedTask;
    }
    public class RetainedReactor
    {
        public Task Handle(RetainedEvent @event) => Task.CompletedTask;
    }
}
