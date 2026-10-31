// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_a_manual_call_joins_an_in_flight_run : given.a_registered_store
{
    TaskCompletionSource _started;
    TaskCompletionSource<CommandResult> _firstReply;
    TaskCompletionSource<CommandResult> _secondReply;
    TaskCompletionSource _rerunStarted;
    bool _duringRerun;
    Task _run;
    Task _joined;
    int _attempt;

    void Establish()
    {
        // This spec drives a manual join after the connection flag changes, without automatic callback scheduling.
        _connection.Lifecycle.OnConnected -= _store.RegisterAfterConnect;
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _rerunStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref _attempt) == 1)
            {
                _started.TrySetResult();
                return _firstReply.Task;
            }

            _rerunStarted.TrySetResult();
            return _secondReply.Task;
        });
    }

    async Task Because()
    {
        _run = _store.RegisterAll();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _connection.Lifecycle.Disconnected();

        var reconnect = _connection.Lifecycle.Connected();
        _joined = _store.RegisterAll();
        _firstReply.SetResult(CommandResult.Success(Guid.NewGuid()));
        await _rerunStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _duringRerun = _store.Registration.IsRegistering;
        _secondReply.SetResult(CommandResult.Success(Guid.NewGuid()));
        await Task.WhenAll(_run, reconnect).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_join_the_shared_flight() => _joined.ShouldBeSame(_run);
    [Fact] void should_register_once_more_for_the_reconnected_kernel() => _attempt.ShouldEqual(2);
    [Fact] void should_remain_registering_during_the_rerun() => _duringRerun.ShouldBeTrue();
    [Fact] void should_clear_registering_after_the_new_run() => _store.Registration.IsRegistering.ShouldBeFalse();
    [Fact] void should_publish_a_successful_outcome() => _store.Registration.IsSuccess.ShouldBeTrue();
}
