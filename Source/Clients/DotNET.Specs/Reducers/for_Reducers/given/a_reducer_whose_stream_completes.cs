// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.given;

/// <summary>
/// A reducer whose first observation stream the spec ends, with re-registration work captured instead of run.
/// </summary>
public class a_reducer_whose_stream_completes : all_dependencies
{
    protected readonly Subject<ReduceOperationMessage> _firstStream = new();
    protected readonly CancellationTokenSource _handlerCancellation = new();
    protected readonly List<Func<Task>> _reconnectWork = [];
    protected int _registrationsAfterCompletion;
    bool _firstStreamCompleted;

    protected override TimeSpan ReconnectDelay => TimeSpan.Zero;

    protected override Func<Func<Task>, Task> ScheduleReconnectWork => work =>
    {
        _reconnectWork.Add(work);
        return Task.CompletedTask;
    };

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(CompletingReducer));
        handler.ReadModelType.Returns(typeof(ReadModel));
        handler.Id.Returns((ReducerId)"completing-reducer");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.IsActive.Returns(true);
        handler.CancellationToken.Returns(_handlerCancellation.Token);
        _handlersByModelType[typeof(ReadModel)] = handler;
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>())
            .Returns(_ => _firstStreamCompleted ? ReRegister() : _firstStream);
    }

    void Destroy()
    {
        _firstStream.Dispose();
        _handlerCancellation.Dispose();
    }

    protected async Task CompleteTheStreamAndRunTheCapturedWork()
    {
        await _reducers.Register();
        EndTheFirstStream();
        foreach (var work in _reconnectWork.ToArray())
        {
            await work();
        }
    }

    protected void EndTheFirstStream()
    {
        _firstStreamCompleted = true;
        _firstStream.OnCompleted();
    }

    protected virtual IObservable<ReduceOperationMessage> ReRegister()
    {
        _registrationsAfterCompletion++;
        return Observable.Never<ReduceOperationMessage>();
    }

    protected bool HasErrorLog() =>
        _logger.ReceivedCalls().Any(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log) &&
            call.GetArguments() is [LogLevel.Error, ..]);

    protected class CompletingReducer;

    protected record ReadModel;
}
