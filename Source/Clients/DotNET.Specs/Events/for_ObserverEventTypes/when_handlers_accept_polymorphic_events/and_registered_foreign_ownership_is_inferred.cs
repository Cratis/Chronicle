// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes.when_handlers_accept_polymorphic_events;

public class and_registered_foreign_ownership_is_inferred : an_explicit_store
{
    Reactors.IReactorHandler _reactor;
    Reducers.IReducerHandler _reducer;
    string? _reactorStore;
    string? _reducerStore;
    IEnumerable<EventTypeId> _reactorIds;
    IEnumerable<EventTypeId> _reducerIds;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<RegisteredEvent>(_ => _.WithId("polymorphic-event").ForEventStore("foreign"))
            .RegisterReactor<Observer>().RegisterReducer<Reducer>();
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _reactor = _store.Reactors.GetHandlerFor(typeof(Observer));
        _reducer = _store.Reducers.GetHandlerFor(typeof(Reducer));
        _reactorStore = ObserverEventTypes.GetSourceStore(typeof(Observer), _store.EventTypes, _store.Name, _reactor.EventTypes);
        _reducerStore = ObserverEventTypes.GetSourceStore(typeof(Reducer), _store.EventTypes, _store.Name, _reducer.EventTypes);
        _reactorIds = ObserverEventTypes.GetIds(_reactor.EventTypes);
        _reducerIds = ObserverEventTypes.GetIds(_reducer.EventTypes);
    }

    [Fact] void should_infer_the_reactors_foreign_inbox_from_concrete_dispatch_events() => _reactor.EventSequenceId.Value.ShouldEqual("inbox-foreign");
    [Fact] void should_infer_the_reducers_foreign_inbox_from_concrete_dispatch_events() => _reducer.EventSequenceId.Value.ShouldEqual("inbox-foreign");
    [Fact] void should_include_the_reactor_in_external_subscription_collection() => _reactorStore.ShouldEqual("foreign");
    [Fact] void should_include_the_reducer_in_external_subscription_collection() => _reducerStore.ShouldEqual("foreign");
    [Fact] void should_collect_the_reactors_registered_concrete_event_id() => _reactorIds.ShouldContainOnly(new EventTypeId("polymorphic-event"));
    [Fact] void should_collect_the_reducers_registered_concrete_event_id() => _reducerIds.ShouldContainOnly(new EventTypeId("polymorphic-event"));

    public interface IContract;
    public record RegisteredEvent(string Name) : IContract;
    public record Model;
    public class Observer
    {
        public void Handle(IContract @event) { }
    }
    public class Reducer
    {
        public Model Handle(IContract @event, Model? current) => current ?? new();
    }
}
