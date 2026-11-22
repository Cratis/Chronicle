// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationQueue;

public class when_every_caller_of_a_waiting_registration_leaves : Specification
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RegistrationQueue _queue;
    CancellationTokenSource _first;
    CancellationTokenSource _second;
    Task _blocker;
    Task _firstCaller;
    Task _secondCaller;
    bool _started;
    int _waitingBeforeLeaving;
    int _waitingAfterLeaving;

    void Establish()
    {
        _queue = new(1, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5), new ManualTimeProvider());
        _first = new();
        _second = new();
    }

    async Task Because()
    {
        _blocker = _queue.Run("blocker", _ => _release.Task);
        _firstCaller = _queue.Run("waiting", _ =>
        {
            _started = true;
            return Task.CompletedTask;
        }, _first.Token);
        _secondCaller = _queue.Run("waiting", _ =>
        {
            _started = true;
            return Task.CompletedTask;
        }, _second.Token);
        _waitingBeforeLeaving = _queue.Waiting;
        await _first.CancelAsync();
        await _second.CancelAsync();
        _waitingAfterLeaving = _queue.Waiting;
        _release.SetResult();
        await _blocker;
    }

    [Fact] void should_have_queued_the_registration_once() => _waitingBeforeLeaving.ShouldEqual(1);
    [Fact] void should_drop_it_from_the_queue() => _waitingAfterLeaving.ShouldEqual(0);
    [Fact] void should_never_start_it() => _started.ShouldBeFalse();
    [Fact] void should_cancel_the_first_caller() => _firstCaller.IsCanceled.ShouldBeTrue();
    [Fact] void should_cancel_the_second_caller() => _secondCaller.IsCanceled.ShouldBeTrue();
}
