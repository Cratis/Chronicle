// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_exit_observations_compete : Specification
{
    readonly TaskCompletionSource<long?> _firstInspect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<long?> _secondInspect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<string> _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _readingLogs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _first;
    Exception? _second;
    Exception? _next;
    int _inspections;
    int _logs;
    int _resets;
    int _delays;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => ++_inspections == 1 ? _firstInspect.Task : _secondInspect.Task,
            _ =>
            {
                _logs++;
                _readingLogs.SetResult();
                return _tail.Task;
            },
            _ =>
            {
                _delays++;
                return Task.CompletedTask;
            });
        var first = Catch.Exception(() => guard.Check(CancellationToken.None));
        var second = Catch.Exception(() => guard.Check(CancellationToken.None));
        _firstInspect.SetResult(137);
        await _readingLogs.Task;
        var next = Catch.Exception(() => guard.Run(
            _ =>
            {
                _resets++;
                return Task.CompletedTask;
            },
            () => true));
        _secondInspect.SetResult(0);
        _tail.SetResult("Out of memory.");
        _first = await first;
        _second = await second;
        _next = await next;
    }

    [Fact] void should_keep_the_first_exit() => _first!.Message.ShouldContain("137: Out of memory.");
    [Fact] void should_share_the_causal_exception() => _second.ShouldEqual(_first);
    [Fact] void should_latch_before_diagnostics_complete() => _next.ShouldEqual(_first);
    [Fact] void should_capture_only_one_tail() => _logs.ShouldEqual(1);
    [Fact] void should_not_reset_while_diagnostics_are_pending() => _resets.ShouldEqual(0);
    [Fact] void should_not_retry() => _delays.ShouldEqual(0);
}
