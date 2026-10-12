// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes.when_handlers_accept_polymorphic_events;

public class and_scalar_returning_helpers_are_not_handlers : an_explicit_store
{
    Reactors.IReactorHandler _reactor;
    Reducers.IReducerHandler _reducer;
    IEventStoreSubscriptions _subscriptions;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<LocalEvent>(_ => _.WithId("local-event").ForEventStore("current"))
            .RegisterEventType<ForeignEvent>(_ => _.WithId("foreign-event").ForEventStore("foreign"))
            .RegisterReactor<Observer>().RegisterReducer<Reducer>();
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();
        _store = CreateStore();
        typeof(EventStore).GetField("<Subscriptions>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_store, _subscriptions);
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _reactor = _store.Reactors.GetHandlerFor(typeof(Observer));
        _reducer = _store.Reducers.GetHandlerFor(typeof(Reducer));
        var collectSubscriptions = typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<Task>>(_store);
        await collectSubscriptions();
    }

    [Fact] void should_keep_the_reactor_on_the_local_event_log() => _reactor.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_keep_the_reducer_on_the_local_event_log() => _reducer.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_match_the_reactors_actual_dispatch_event_set() => ObserverEventTypes.GetIds(_reactor.EventTypes).ShouldContainOnly(new EventTypeId("local-event"));
    [Fact] void should_match_the_reducers_actual_dispatch_event_set() => ObserverEventTypes.GetIds(_reducer.EventTypes).ShouldContainOnly(new EventTypeId("local-event"));
    [Fact] void should_not_collect_a_foreign_subscription_from_either_helper() => _subscriptions.ReceivedCalls().ShouldBeEmpty();

    public interface IContract;
    public record LocalEvent(string Name);
    public record ForeignEvent(string Name) : IContract;
    public record Model;
    public class Observer
    {
        public Task Handle(LocalEvent @event) => Task.CompletedTask;
        bool ShouldProcess(IContract @event) => false;
    }
    public class Reducer
    {
        public Model Handle(LocalEvent @event, Model? current) => current ?? new();
        bool ShouldProcess(IContract @event, Model? current) => false;
    }
}
