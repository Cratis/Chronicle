// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_connection_stays_disconnected : given.a_registered_store
{
    TaskCompletionSource _started;
    TaskCompletionSource<CommandResult> _reply;

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
        var run = _store.RegisterAll();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _connection.Lifecycle.Disconnected();
        _reply.SetResult(CommandResult.Success(Guid.NewGuid()));
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_leave_registration_pending_for_reconnect() => _store.Registration.IsRegistering.ShouldBeTrue();
    [Fact] void should_not_rerun_without_a_connection() => _services.EventStores.Received(1).EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
}
