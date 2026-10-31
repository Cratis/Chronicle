// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes.when_handlers_accept_polymorphic_events;

public class and_custom_side_effect_returns_remain_handlers : an_explicit_store
{
    Reactors.IReactorHandler _reactor;
    IEventStoreSubscriptions _subscriptions;
    IEventStoreSubscriptionBuilder _builder;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<ForeignEvent>(_ => _.WithId("custom-effect-event").ForEventStore("foreign"))
            .RegisterReactor<Observer>();
        _registeredSideEffectHandlers.CanHandleReturnType(typeof(CustomEffect)).Returns(true);
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();
        _builder = Substitute.For<IEventStoreSubscriptionBuilder>();
        _subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>()).Returns(call =>
        {
            call.Arg<Action<IEventStoreSubscriptionBuilder>>()(_builder);
            return Task.CompletedTask;
        });
        _store = CreateStore();
        typeof(EventStore).GetField("<Subscriptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, _subscriptions);
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _reactor = _store.Reactors.GetHandlerFor(typeof(Observer));
        var collectSubscriptions = typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<Task>>(_store);
        await collectSubscriptions();
    }

    [Fact] void should_keep_custom_synchronous_return_classification_for_sequence_inference() => _reactor.EventSequenceId.Value.ShouldEqual("inbox-foreign");
    [Fact] void should_keep_the_native_dispatch_event_identity() => ObserverEventTypes.GetIds(_reactor.EventTypes).ShouldContainOnly(new EventTypeId("custom-effect-event"));
    [Fact] void should_collect_the_foreign_subscription_for_the_custom_handler() => _subscriptions.Received(1).Subscribe(new EventStoreSubscriptionId("foreign"), "foreign", Arg.Any<Action<IEventStoreSubscriptionBuilder>>());
    [Fact] void should_filter_the_subscription_by_the_native_dispatch_event_identity() => _builder.Received(1).WithEventType(new EventTypeId("custom-effect-event"));

    public interface IContract;
    public record ForeignEvent(string Name) : IContract;
    public record CustomEffect;
    public class Observer
    {
        public CustomEffect Handle(IContract @event) => new();
    }
}
