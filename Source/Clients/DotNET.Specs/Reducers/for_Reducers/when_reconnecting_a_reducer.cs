// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_reconnecting_a_reducer : given.all_dependencies
{
    readonly List<string> _programIdentifiers = [];
    readonly Subject<ReduceOperationMessage> _stream = new();
    readonly TaskCompletionSource _reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(ReconnectedReducer));
        handler.ReadModelType.Returns(typeof(ReconnectedReadModel));
        handler.Id.Returns((ReducerId)"reconnected");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.IsActive.Returns(true);
        _handlersByModelType[typeof(ReconnectedReadModel)] = handler;
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message =>
            {
                _programIdentifiers.Add(message.Content.Value0!.ProgramIdentifier);
                if (_programIdentifiers.Count == 2)
                {
                    _reconnected.TrySetResult();
                }
            });
            return _programIdentifiers.Count == 1 ? _stream : Observable.Never<ReduceOperationMessage>();
        });
    }

    async Task Because()
    {
        await _reducers.Register();
        _stream.OnError(new IOException("Disconnected"));
        await _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _stream.Dispose();

    [Fact] void should_send_the_program_identifier_on_initial_registration() => _programIdentifiers[0].ShouldEqual("reducer-program");
    [Fact] void should_send_the_program_identifier_on_reconnect() => _programIdentifiers.ShouldEqual(["reducer-program", "reducer-program"]);

    class ReconnectedReducer;
    record ReconnectedReadModel;
}
