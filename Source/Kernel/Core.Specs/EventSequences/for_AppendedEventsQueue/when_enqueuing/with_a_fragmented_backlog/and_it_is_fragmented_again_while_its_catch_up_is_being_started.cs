// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_fragmented_backlog;

/// <summary>
/// An observer that re-subscribes and is spilled again while the call that starts its catch-up is still pending never has
/// two catch-up calls at once, but is not forgotten either: once the first call completes, catch-up is started again
/// so the second spill is followed by a call that began after it.
/// </summary>
public class and_it_is_fragmented_again_while_its_catch_up_is_being_started : given.a_fragmented_backlog
{
    readonly TaskCompletionSource _firstCatchUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _rerun = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _catchUps;
    int _catchUpsBeforeRelease;
    bool _rerunStarted;

    void Establish() => _observer.CatchUp().Returns(
        _ =>
        {
            Interlocked.Increment(ref _catchUps);
            return _firstCatchUp.Task;
        },
        _ =>
        {
            Interlocked.Increment(ref _catchUps);
            _rerun.TrySetResult();
            return Task.CompletedTask;
        });

    async Task Because()
    {
        await EnqueueAndDeplete(Alternating(Limit + 1, _eventType));
        await _queue.Subscribe(_observerKey, [_eventType]);
        await EnqueueAndDeplete(Alternating(Limit + 1, _eventType, (ulong)Limit + 1));
        _catchUpsBeforeRelease = Volatile.Read(ref _catchUps);
        _firstCatchUp.SetResult();

        try
        {
            await _rerun.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _rerunStarted = true;
        }
        catch (TimeoutException)
        {
            _rerunStarted = false;
        }
    }

    [Fact] void should_not_start_a_second_catch_up_while_the_first_is_pending() => _catchUpsBeforeRelease.ShouldEqual(1);
    [Fact] void should_catch_up_again_once_the_first_completes() => _rerunStarted.ShouldBeTrue();
    [Fact] void should_catch_up_exactly_twice() => _observer.Received(2).CatchUp();
    [Fact] void should_meter_both_spills() => _spillRecorder.Reasons.SequenceEqual(["FragmentedBacklog", "FragmentedBacklog"]).ShouldBeTrue();
}
