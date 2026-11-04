// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class a_connected_reducers : for_Reducers.given.all_dependencies
{
    protected IConnectionLifecycle _lifecycle;

    void Establish()
    {
        _lifecycle = _eventStore.Connection.Lifecycle;
        _lifecycle.IsConnected.Returns(true);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _clientArtifacts.Reducers.Returns([]);
        _eventTypes.AllClrTypes.Returns([typeof(Changed)]);
        _eventTypes.GetEventTypeFor(typeof(Changed)).Returns(new EventType("changed", 1));
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        _eventStore.Reducers.Returns(_reducers);
    }

    void Destroy()
    {
        foreach (var handler in _reducers.GetAllHandlers())
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    [EventType]
    public record Changed;

    public record First
    {
        public static First Reduce(Changed @event, First? current) => current ?? new();
    }

    public record Second
    {
        public static Second Reduce(Changed @event, Second? current) => current ?? new();
    }

    public record ThirdModel;

    public class ThirdReducer : IReducerFor<ThirdModel>
    {
        public ThirdModel Reduce(Changed @event, ThirdModel? current) => current ?? new();
    }
}
