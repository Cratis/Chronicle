// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_disconnect_is_still_running : given.a_registered_store
{
    readonly FenceLogger _logger = new();
    readonly TaskCompletionSource _disconnectEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDisconnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _registrationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _disconnectCompletedWhenRegistrationStarted;
    int _registrationsWhileFenced;

    protected override ILogger<ConnectionLifecycle> LifecycleLogger => _logger;

    void Establish()
    {
        _connection.Lifecycle.OnDisconnected += async () =>
        {
            _disconnectEntered.TrySetResult();
            await _releaseDisconnect.Task;
        };
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            _disconnectCompletedWhenRegistrationStarted = _releaseDisconnect.Task.IsCompleted;
            _registrationStarted.TrySetResult();
            return Task.FromResult(CommandResult.Success(Guid.NewGuid()));
        });
    }

    async Task Because()
    {
        var disconnecting = _connection.Lifecycle.Disconnected();
        await _disconnectEntered.Task;
        var connecting = _connection.Lifecycle.Connected();

        // Whichever happens first releases the disconnect, so the spec finishes on broken code too and the facts report it.
        await Task.WhenAny(_logger.Fenced, _registrationStarted.Task);
        _registrationsWhileFenced = _services.EventStores.ReceivedCalls().Count();
        _releaseDisconnect.TrySetResult();
        await Task.WhenAll(disconnecting, connecting);
    }

    [Fact] void should_not_register_until_the_disconnect_has_completed() => _disconnectCompletedWhenRegistrationStarted.ShouldBeTrue();
    [Fact] void should_not_register_while_the_disconnect_is_running() => _registrationsWhileFenced.ShouldEqual(0);
    [Fact] void should_register_once_after_the_disconnect() => _services.EventStores.Received(1).EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_publish_a_successful_registration() => _store.Registration.IsSuccess.ShouldBeTrue();

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
