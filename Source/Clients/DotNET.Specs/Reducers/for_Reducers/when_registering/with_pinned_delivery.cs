// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class with_pinned_delivery : given.all_dependencies
{
    ReducerDefinition _definition;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;
    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(PinnedReducer));
        handler.ReadModelType.Returns(typeof(PinnedReadModel));
        handler.Id.Returns((ReducerId)"pinned");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.IsActive.Returns(true);
        _handlersByModelType[typeof(PinnedReadModel)] = handler;
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _definition = message.Content.Value0!.Reducer);
            return Observable.Never<ReduceOperationMessage>();
        });
    }
    async Task Because() => await _reducers.Register();
    [Fact] void should_send_pinned_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);

    class PinnedReducer;
    record PinnedReadModel;
}
