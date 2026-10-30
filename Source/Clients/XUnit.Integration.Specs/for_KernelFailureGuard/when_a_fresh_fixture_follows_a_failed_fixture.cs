// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_a_fresh_fixture_follows_a_failed_fixture : Specification
{
    Exception? _previous;
    Exception? _fresh;
    int _resets;
    int _logs;
    bool _monitorJoined;

    async Task Because()
    {
        var failed = new KernelFailureGuard(
            "previous-kernel",
            _ => Task.FromResult<long?>(137),
            _ => Task.FromResult("Out of memory."),
            _ => Task.CompletedTask);
        _previous = await Catch.Exception(() => failed.Run(_ => Task.CompletedTask, () => true));
        var fresh = new KernelFailureGuard(
            "fresh-kernel",
            _ => Task.FromResult<long?>(null),
            _ =>
            {
                _logs++;
                return Task.FromResult("unused");
            },
            WaitForCancellation);
        _fresh = await Catch.Exception(() => fresh.Run(
            _ =>
            {
                _resets++;
                return Task.CompletedTask;
            },
            () => false));
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
            _monitorJoined = true;
        }
    }

    [Fact] void should_have_failed_the_previous_fixture() => _previous.ShouldBeOfExactType<KernelProcessExited>();
    [Fact] void should_not_leak_the_previous_fixture_latch() => _fresh.ShouldBeNull();
    [Fact] void should_reset_the_fresh_fixture() => _resets.ShouldEqual(1);
    [Fact] void should_not_collect_diagnostics_from_the_fresh_fixture() => _logs.ShouldEqual(0);
    [Fact] void should_join_the_fresh_monitor() => _monitorJoined.ShouldBeTrue();
}
