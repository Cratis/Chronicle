// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_a_running_kernel_has_a_transient_reset_failure : Specification
{
    Exception? _failure;
    int _resets;
    int _retries;
    int _monitorsStopped;
    int _logs;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromResult<long?>(null),
            _ =>
            {
                _logs++;
                return Task.FromResult("unused");
            },
            WaitForCancellation);
        _failure = await Catch.Exception(() => guard.Run(
            _ => ++_resets == 1 ? Task.FromException(new IOException("transient")) : Task.CompletedTask,
            () => true,
            () =>
            {
                _retries++;
                return Task.CompletedTask;
            }));
    }

    async Task WaitForCancellation(CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = cancellationToken.Register(() => completion.SetCanceled(cancellationToken));
        try
        {
            await completion.Task;
        }
        finally
        {
            _monitorsStopped++;
        }
    }

    [Fact] void should_succeed_on_retry() => _failure.ShouldBeNull();
    [Fact] void should_retry_once() => _retries.ShouldEqual(1);
    [Fact] void should_run_two_reset_attempts() => _resets.ShouldEqual(2);
    [Fact] void should_join_every_monitor_before_returning() => _monitorsStopped.ShouldEqual(2);
    [Fact] void should_not_capture_exit_diagnostics() => _logs.ShouldEqual(0);
}
