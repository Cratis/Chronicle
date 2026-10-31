// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_the_kernel_has_exited : Specification
{
    KernelFailureGuard _guard;
    Exception? _first;
    Exception? _second;
    int _inspections;
    int _logs;
    int _resets;
    int _delays;

    void Establish() => _guard = new(
        "kernel-123",
        _ => Task.FromResult<long?>(++_inspections == 1 ? 137 : 0),
        _ => Task.FromResult(++_logs == 1 ? "Out of memory." : "Stack overflow."),
        _ =>
        {
            _delays++;
            return Task.CompletedTask;
        });

    async Task Because()
    {
        _first = await Catch.Exception(() => _guard.Run(Reset, () => true));
        _second = await Catch.Exception(() => _guard.Run(Reset, () => true));
    }

    Task Reset(CancellationToken cancellationToken)
    {
        _resets++;
        return Task.CompletedTask;
    }

    [Fact] void should_report_the_exit() => _first.ShouldBeOfExactType<KernelProcessExited>();
    [Fact] void should_rethrow_the_same_first_cause() => _second.ShouldEqual(_first);
    [Fact] void should_keep_the_initial_exit_and_tail() => _second!.Message.ShouldContain("137: Out of memory.");
    [Fact] void should_include_the_container_identity() => _first!.Message.ShouldContain("kernel-123");
    [Fact] void should_inspect_only_once() => _inspections.ShouldEqual(1);
    [Fact] void should_collect_logs_only_once() => _logs.ShouldEqual(1);
    [Fact] void should_not_start_reset() => _resets.ShouldEqual(0);
    [Fact] void should_not_sleep() => _delays.ShouldEqual(0);
}
