// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_current_and_generation_handlers : given.all_dependencies
{
    readonly List<RegisterReducer> _registrations = [];
    IReducerHandler _handler;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(Current), typeof(Previous)]);
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _eventTypes.AllClrTypes.Returns(eventTypes.AllClrTypes);
        _eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => eventTypes.GetEventTypeFor(call.Arg<Type>()));
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _registrations.Add(message.Content.Value0!));
            return Observable.Empty<ReduceOperationMessage>();
        });
    }

    async Task Because() => _handler = await _reducers.Register<MixedReducer, Model>();

    [Fact] void should_observe_only_the_current_event_type() => _handler.EventTypes.ShouldContainOnly(typeof(Current).GetEventType());
    [Fact] void should_register_with_the_kernel() => _registrations.Count.ShouldEqual(1);
    [Fact] void should_register_only_the_current_event_type_with_the_kernel() => _registrations[0].Reducer.EventTypes.Select(_ => (_.EventType.Id, _.EventType.Generation)).ShouldContainOnly((typeof(Current).GetEventType().Id.Value, 2u));

    [EventType(generation: 2)]
    public record Current(string Name);
    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    public record Model;
    public class MixedReducer : IReducerFor<Model>
    {
        public Model Handle(Current @event, Model? current) => new();
        public Model Handle(Previous @event, Model? current) => new();
    }
}
