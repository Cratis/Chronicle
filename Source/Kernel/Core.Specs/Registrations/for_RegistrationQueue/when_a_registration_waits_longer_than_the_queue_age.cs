// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationQueue;

public class when_a_registration_waits_longer_than_the_queue_age : Specification
{
    static readonly TimeSpan _maxAge = TimeSpan.FromSeconds(20);
    static readonly TimeSpan _retryAfter = TimeSpan.FromSeconds(7);

    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ManualTimeProvider _clock;
    RegistrationQueue _queue;
    Task _running;
    Task _tooLate;
    Task _youngEnough;
    Exception _failure;
    bool _tooLateStarted;
    bool _runningStillRunning;

    void Establish()
    {
        _clock = new();
        _queue = new(1, _maxAge, _retryAfter, _clock);
    }

    async Task Because()
    {
        _running = _queue.Run("running", _ => _release.Task);
        _tooLate = _queue.Run("too-late", _ =>
        {
            _tooLateStarted = true;
            return Task.CompletedTask;
        });
        _clock.Advance(_maxAge - TimeSpan.FromSeconds(1));
        _youngEnough = _queue.Run("young-enough", _ => Task.CompletedTask);
        _clock.Advance(TimeSpan.FromSeconds(2));
        _failure = await Catch.Exception(async () => await _tooLate);
        _runningStillRunning = !_running.IsCompleted;
        _release.SetResult();
        await _running;
        await _youngEnough;
    }

    [Fact] void should_answer_busy() => _failure.ShouldBeOfExactType<RegistrationBusyException>();
    [Fact] void should_say_when_to_retry() => ((RegistrationBusyException)_failure).RetryAfter.ShouldEqual(_retryAfter);
    [Fact] void should_not_start_the_registration() => _tooLateStarted.ShouldBeFalse();
    [Fact] void should_leave_the_running_registration_alone() => _runningStillRunning.ShouldBeTrue();
    [Fact] void should_still_apply_a_registration_that_waited_less_than_the_age() => _youngEnough.IsCompletedSuccessfully.ShouldBeTrue();
}
