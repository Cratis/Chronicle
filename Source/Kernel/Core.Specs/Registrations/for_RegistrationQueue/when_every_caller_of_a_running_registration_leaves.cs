// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationQueue;

public class when_every_caller_of_a_running_registration_leaves : Specification
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RegistrationQueue _queue;
    CancellationTokenSource _first;
    CancellationTokenSource _second;
    Task _firstCaller;
    Task _secondCaller;
    bool _cancelledAfterFirstLeft;
    bool _cancelledAfterSecondLeft;
    bool _workStopped;

    void Establish()
    {
        _queue = new(1, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5), new ManualTimeProvider());
        _first = new();
        _second = new();
    }

    async Task Because()
    {
        var token = CancellationToken.None;
        async Task Work(CancellationToken cancellation)
        {
            token = cancellation;
            _started.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellation);
            }
            catch (OperationCanceledException)
            {
                _workStopped = true;
                throw;
            }
        }

        _firstCaller = _queue.Run("running", Work, _first.Token);
        await _started.Task;
        _secondCaller = _queue.Run("running", Work, _second.Token);
        await _first.CancelAsync();
        _cancelledAfterFirstLeft = token.IsCancellationRequested;
        await _second.CancelAsync();
        _cancelledAfterSecondLeft = token.IsCancellationRequested;
        for (var attempt = 0; attempt < 100 && _queue.Running > 0; attempt++)
        {
            await Task.Delay(50);
        }
    }

    [Fact] void should_keep_the_work_going_while_another_caller_waits() => _cancelledAfterFirstLeft.ShouldBeFalse();
    [Fact] void should_cancel_the_work_when_the_last_caller_leaves() => _cancelledAfterSecondLeft.ShouldBeTrue();
    [Fact] void should_stop_the_work() => _workStopped.ShouldBeTrue();
    [Fact] void should_leave_nothing_running() => _queue.Running.ShouldEqual(0);
}
