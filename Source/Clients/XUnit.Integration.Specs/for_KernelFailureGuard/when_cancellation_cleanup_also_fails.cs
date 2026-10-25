// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_cancellation_cleanup_also_fails : Specification
{
    readonly TaskCompletionSource _probe = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;
    int _inspections;
    int _retries;
    bool _resetJoined;
    bool _monitorJoined;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromResult<long?>(++_inspections == 1 ? null : 137),
            _ => Task.FromResult("Out of memory."),
            async cancellationToken =>
            {
                try
                {
                    await _probe.Task.WaitAsync(cancellationToken);
                }
                finally
                {
                    _monitorJoined = true;
                }
            });
        var run = Catch.Exception(() => guard.Run(
            Reset,
            () => true,
            () =>
            {
                _retries++;
                return Task.CompletedTask;
            }));
        _probe.SetResult();
        _failure = await run;
    }

    async Task Reset(CancellationToken cancellationToken)
    {
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var brokenCleanup = cancellationToken.Register(() => throw new IOException("secondary disposal error"));
        await using var registration = cancellationToken.Register(cancelled.SetResult);
        await cancelled.Task;
        _resetJoined = true;
        throw new IOException("secondary RPC failure");
    }

    [Fact] void should_keep_the_container_exit_as_primary() => _failure.ShouldBeOfExactType<KernelProcessExited>();
    [Fact] void should_keep_the_initial_exit_details() => _failure!.Message.ShouldContain("137: Out of memory.");
    [Fact] void should_join_the_cancelled_rpc_despite_a_callback_failure() => _resetJoined.ShouldBeTrue();
    [Fact] void should_join_the_monitor() => _monitorJoined.ShouldBeTrue();
    [Fact] void should_not_retry() => _retries.ShouldEqual(0);
}
