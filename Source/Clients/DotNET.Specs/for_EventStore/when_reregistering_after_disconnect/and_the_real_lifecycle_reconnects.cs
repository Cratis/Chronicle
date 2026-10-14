// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_real_lifecycle_reconnects : given.a_registered_store
{
    TaskCompletionSource _started;
    TaskCompletionSource<CommandResult> _reply;
    RegistrationOutcome _during;

    void Establish()
    {
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            _started.TrySetResult();
            return _reply.Task;
        });
    }

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        var reconnect = _connection.Lifecycle.Connected();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _during = _store.Registration;
        _reply.SetResult(CommandResult.Success(Guid.NewGuid()));
        await reconnect.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_remain_registering_during_the_connected_callback() => _during.IsRegistering.ShouldBeTrue();
    [Fact] void should_retain_the_previous_run_while_registering() => (_during with { IsRegistering = false }).ShouldEqual(_previous);
    [Fact] void should_clear_registering_after_reconnection_finishes() => _store.Registration.IsRegistering.ShouldBeFalse();
    [Fact] void should_publish_the_reconnected_registration_as_successful() => _store.Registration.IsSuccess.ShouldBeTrue();
    [Fact] void should_register_once_for_the_reconnected_kernel() => _services.EventStores.Received(1).EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
}
