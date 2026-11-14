// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.given;

public class a_reconnecting_reducer : all_dependencies
{
    protected readonly List<Subject<ReduceOperationMessage>> _streams = [];
    protected readonly List<RegisterReducer> _registrations = [];
    protected readonly List<Func<Task>> _reconnectWork = [];
    protected readonly TaskCompletionSource<ReducerResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected IReducerHandler _original;
    protected CancellationToken _originalToken;
    readonly CompositeDisposable _outgoing = new();
    System.Diagnostics.ActivitySource _traceSource;

    protected override TimeSpan ReconnectDelay => TimeSpan.Zero;
    protected override Func<Func<Task>, Task> ScheduleReconnectWork => work =>
    {
        _reconnectWork.Add(work);
        return Task.CompletedTask;
    };

    async Task Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _traceSource = new("reconnecting-reducer-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reducers>(_traceSource);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _clientArtifacts.Reducers.Returns([typeof(Model)]);
        _eventTypes.AllClrTypes.Returns([typeof(Changed)]);
        _eventTypes.GetEventTypeFor(typeof(Changed)).Returns(new EventType("changed", 1));
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(Changed));
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _outgoing.Add(call.Arg<IObservable<ReducerMessage>>().Subscribe(message =>
            {
                if (message.Content.Value is RegisterReducer registration) _registrations.Add(registration);
                if (message.Content.Value is ReducerResult result) _result.TrySetResult(result);
            }));
            var stream = new Subject<ReduceOperationMessage>();
            _streams.Add(stream);
            return stream;
        });
        _reducers = CreateReducers();
        await _reducers.Discover();
        await _reducers.Register();
        _original = _reducers.GetHandlerFor(typeof(Model));
        _originalToken = _original.CancellationToken;
    }

    protected void Disconnect() => _eventStore.Connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();

    protected async Task ReconnectAndDeliver()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        await _reducers.Register();
        _streams[^1].OnNext(new ReduceOperationMessage
        {
            Partition = "model",
            DeliveryId = 42,
            Events = [new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with { EventType = new("changed", 1), SequenceNumber = 12 }).ToContract(),
                Content = "{\"Name\":\"recovered\"}"
            }]
        });
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected bool HasErrorLog() => _logger.ReceivedCalls().Any(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments() is [LogLevel.Error, ..]);

    void Destroy()
    {
        _reducers.Release();
        _outgoing.Dispose();
        foreach (var stream in _streams) stream.Dispose();
        _traceSource.Dispose();
    }

    [EventType] public record Changed(string Name);
    public record Model(string Name)
    {
        public static Model Reduce(Changed @event, Model? current) => new(@event.Name);
    }
}
