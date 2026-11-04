// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

/// <summary>
/// A registered reactor whose first observation stream the spec ends, with re-registration work captured instead of run.
/// </summary>
public class a_reactor_whose_stream_completes : all_dependencies
{
    protected readonly Subject<EventsToObserve> _firstStream = new();
    protected readonly List<Func<Task>> _reconnectWork = [];
    protected IReactorHandler _handler;
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
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(_ => _firstStreamCompleted ? ReRegister() : _firstStream);
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _handler = _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    protected async Task CompleteTheStreamAndRunTheCapturedWork()
    {
        await _reactors.Register();
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

    protected virtual IObservable<EventsToObserve> ReRegister()
    {
        _registrationsAfterCompletion++;
        return System.Reactive.Linq.Observable.Never<EventsToObserve>();
    }

    protected bool HasErrorLog() =>
        _logger.ReceivedCalls().Any(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log) &&
            call.GetArguments() is [LogLevel.Error, ..]);

    protected class MyReactor : IReactor;
}
