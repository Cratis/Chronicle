// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

/// <summary>
/// A replacement subscription that is spilled while the call that started the observer's catch-up is still in flight must
/// not be swallowed by the trigger that is already running. There is never a second concurrent catch-up call for the
/// observer, but once the in-flight call completes the catch-up is started again, so the later spill is followed by a call
/// that began after it. A spill after every call completed starts a new trigger rather than finding a leaked one.
/// </summary>
public class and_a_replacement_subscription_is_spilled_while_catch_up_is_in_flight : given.an_unreachable_observer
{
    readonly TaskCompletionSource _firstCatchUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _secondCatchUpStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _thirdCatchUpStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Lock _inFlightLock = new();
    int _catchUps;
    int _catchUpsWhileFirstInFlight;
    int _inFlight;
    int _maxInFlight;
    bool _secondStarted;
    bool _thirdStarted;

    void Establish() => _observer.CatchUp().Returns(
        _ =>
        {
            Interlocked.Increment(ref _catchUps);
            return Track(_firstCatchUp.Task);
        },
        _ =>
        {
            Interlocked.Increment(ref _catchUps);
            _secondCatchUpStarted.TrySetResult();
            return Track(Task.CompletedTask);
        },
        _ =>
        {
            Interlocked.Increment(ref _catchUps);
            _thirdCatchUpStarted.TrySetResult();
            return Track(Task.CompletedTask);
        });

    async Task Because()
    {
        await DispatchFirstEvent();
        await _queue.Subscribe(_observerKey, [_eventType]);
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
        _catchUpsWhileFirstInFlight = Volatile.Read(ref _catchUps);

        _firstCatchUp.SetResult();
        _secondStarted = await Started(_secondCatchUpStarted);

        await _queue.Subscribe(_observerKey, [_eventType]);
        await _queue.Enqueue([_laterEvent with { Context = _laterEvent.Context with { EventSourceId = "third", SequenceNumber = 2UL } }]);
        await _queue.AwaitQueueDepletion(500, 10);
        _thirdStarted = await Started(_thirdCatchUpStarted);
    }

    static async Task<bool> Started(TaskCompletionSource signal)
    {
        try
        {
            await signal.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    async Task Track(Task inner)
    {
        lock (_inFlightLock)
        {
            _inFlight++;
            _maxInFlight = Math.Max(_maxInFlight, _inFlight);
        }

        try
        {
            await inner;
        }
        finally
        {
            lock (_inFlightLock)
            {
                _inFlight--;
            }
        }
    }

    [Fact] void should_not_start_a_second_catch_up_while_the_first_is_in_flight() => _catchUpsWhileFirstInFlight.ShouldEqual(1);
    [Fact] void should_catch_up_again_after_the_in_flight_call_completes() => _secondStarted.ShouldBeTrue();
    [Fact] void should_never_have_two_catch_up_calls_in_flight() => _maxInFlight.ShouldEqual(1);
    [Fact] void should_start_a_new_trigger_for_a_spill_after_the_catch_ups_completed() => _thirdStarted.ShouldBeTrue();
    [Fact] void should_catch_up_once_for_each_spill_that_followed_a_completed_call() => _observer.Received(3).CatchUp();
    [Fact] async Task should_have_spilled_the_replacement_subscription() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
