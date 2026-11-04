// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_is_off_and_the_disconnect_is_still_running : given.a_store_with_an_explicit_reactor
{
    readonly FenceLogger _logger = new();
    readonly TaskCompletionSource _disconnectEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDisconnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reregistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectionId _before;
    ConnectionId _whenReregistered;
    bool _disconnectCompletedWhenReregistered;
    int _observesWhileFenced;

    protected override ILogger<ConnectionLifecycle> LifecycleLogger => _logger;

    void Establish()
    {
        _before = _connection.Lifecycle.ConnectionId;
        _connection.Lifecycle.OnDisconnected += async () =>
        {
            _disconnectEntered.TrySetResult();
            await _releaseDisconnect.Task;
        };
        _services.Reactors.ClearReceivedCalls();
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _disconnectCompletedWhenReregistered = _releaseDisconnect.Task.IsCompleted;
            _whenReregistered = _connection.Lifecycle.ConnectionId;
            _reregistered.TrySetResult();
            return System.Reactive.Linq.Observable.Never<Cratis.Chronicle.Contracts.Observation.EventsToObserve>();
        });
    }

    async Task Because()
    {
        var disconnecting = _connection.Lifecycle.Disconnected();
        await _disconnectEntered.Task;
        var connecting = _connection.Lifecycle.Connected();

        // The fence log or the re-registration, whichever comes first, releases the latch so broken code still finishes.
        await Task.WhenAny(_logger.Fenced, _reregistered.Task);
        _observesWhileFenced = _services.Reactors.ReceivedCalls().Count();
        _releaseDisconnect.TrySetResult();
        await Task.WhenAll(disconnecting, connecting);
    }

    [Fact] void should_not_register_the_reactor_while_the_disconnect_is_running() => _observesWhileFenced.ShouldEqual(0);
    [Fact] void should_register_the_reactor_after_the_disconnect_completed() => _disconnectCompletedWhenReregistered.ShouldBeTrue();
    [Fact] void should_register_the_reactor_after_the_connection_id_rotated() => _whenReregistered.ShouldNotEqual(_before);
    [Fact] void should_register_the_reactor_exactly_once() => _services.Reactors.Received(1).Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());

    sealed class FenceLogger : ILogger<ConnectionLifecycle>
    {
        readonly TaskCompletionSource _fenced = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Fenced => _fenced.Task;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == "WaitingForDisconnectToComplete")
            {
                _fenced.TrySetResult();
            }
        }
    }
}
