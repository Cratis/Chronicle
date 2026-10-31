// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes;

public class when_resolving_registered_handlers : Specification
{
    readonly List<EventSequenceId> _reactorSequences = [];
    readonly List<EventSequenceId> _reducerSequences = [];
    readonly List<EventTypeId[]> _ids = [];
    IEventTypes _foreign;
    IEventTypes _local;

    void Establish()
    {
        _foreign = RegisteredEventTypesForSpecifications.Create((typeof(Foreign), new("registered-foreign", 3)));
        _local = RegisteredEventTypesForSpecifications.Create((typeof(Local), new("registered-local", 2)));
    }

    void Because()
    {
        foreach (var registry in new[] { _foreign, _local, _local, _foreign })
        {
            _reactorSequences.Add(ReactorTypeExtensions.GetEventSequenceId(typeof(Reactor), registry, "current"));
            _reducerSequences.Add(ReducerTypeExtensions.GetEventSequenceId(typeof(Reducer), registry, "current", typeof(Model)));
            _ids.Add(ObserverEventTypes.GetIds(ReactorInvoker.GetEventTypesFor(registry, typeof(Reactor))).ToArray());
        }
    }

    [Fact] void should_resolve_reactor_sequences_per_registry_in_both_orders() =>
        _reactorSequences.ShouldEqual([new EventSequenceId("inbox-other"), EventSequenceId.Log, EventSequenceId.Log, new EventSequenceId("inbox-other")]);
    [Fact] void should_resolve_reducer_sequences_per_registry_in_both_orders() =>
        _reducerSequences.ShouldEqual([new EventSequenceId("inbox-other"), EventSequenceId.Log, EventSequenceId.Log, new EventSequenceId("inbox-other")]);
    [Fact] void should_use_registered_foreign_ids() => _ids[0].ShouldContainOnly(new EventTypeId("registered-foreign"));
    [Fact] void should_use_registered_local_ids() => _ids[1].ShouldContainOnly(new EventTypeId("registered-local"));
    [Fact] void should_preserve_public_reactor_inference() => ReactorTypeExtensions.GetEventSequenceId(typeof(Reactor), "current").ShouldEqual(EventSequenceId.Log);
    [Fact] void should_preserve_public_reducer_inference() => ReducerTypeExtensions.GetEventSequenceId(typeof(Reducer), "current").ShouldEqual(EventSequenceId.Log);

    [EventStore("other")]
    public record Foreign;
    [EventStore("current")]
    public record Local;
    public record Model;
    public class Reactor : IReactor
    {
        public Task On(Foreign @event) => Task.CompletedTask;
        public Task On(Local @event) => Task.CompletedTask;
    }
    public class Reducer : IReducerFor<Model>
    {
        public Model On(Foreign @event, Model? current) => new();
        public Model On(Local @event, Model? current) => new();
    }
}
