// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_stopping;

public class and_an_earlier_stop_fails_during_a_newer_stop : given.a_captures_manager
{
    readonly TaskCompletionSource _firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _failFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseSecond = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TimeoutException _failure = new("First Stop failed");
    int _stops;
    Exception? _error;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Unsubscribe(_eventStore, Arg.Any<CaptureDefinition>()).Returns(async _ =>
        {
            if (++_stops == 1)
            {
                _firstEntered.TrySetResult();
                await _failFirst.Task;
                throw _failure;
            }

            _secondEntered.TrySetResult();
            await _releaseSecond.Task;
        });
    }

    async Task Because()
    {
        var first = Catch.Exception(() => _manager.Stop(_events.Id));
        await _firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var second = _manager.Stop(_events.Id);
        _failFirst.TrySetResult();
        _error = await first.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.Ensure();
        await _manager.EnsureSubscribedIn("new-tenant");
        await _silo.TimerRegistry.FireAllAsync();
        _releaseSecond.TrySetResult();
        await second.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_surface_the_first_failure() => _error.ShouldBeSame(_failure);
    [Fact] async Task should_not_reopen_automatic_recovery_during_the_newer_stop() => await _subscriptions.DidNotReceive().Recover(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
    [Fact] async Task should_not_reopen_namespace_recovery_during_the_newer_stop() => await _subscriptions.DidNotReceive().Recover(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<CaptureDefinition>());
    [Fact] async Task should_finish_stopped() => (await _captures.Get(_events.Id)).Status.ShouldEqual(CaptureStatus.Stopped);
}
