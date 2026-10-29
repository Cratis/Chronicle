// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_the_kernel_exits_during_reset : Specification
{
    readonly TaskCompletionSource _probe = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;
    Exception? _next;
    bool _cancelled;
    int _inspections;
    int _resets;
    int _retries;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromResult<long?>(++_inspections == 1 ? null : 0),
            _ => Task.FromException<string>(new IOException("secret transport diagnostics")),
            _probe.Task.WaitAsync);
        var reset = Catch.Exception(() => guard.Run(
            async cancellationToken =>
            {
                _resets++;
                var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = cancellationToken.Register(() =>
                {
                    _cancelled = true;
                    cancelled.SetResult();
                });
                await cancelled.Task;
                throw new IOException("later RPC disposal failure");
            },
            () => true,
            () =>
            {
                _retries++;
                return Task.CompletedTask;
            }));
        _probe.SetResult();
        _failure = await reset;
        _next = await Catch.Exception(() => guard.Run(
            _ =>
            {
                _resets++;
                return Task.CompletedTask;
            },
            () => true));
    }

    [Fact] void should_treat_zero_exit_as_terminal() => _failure!.Message.ShouldContain("code 0");
    [Fact] void should_make_missing_diagnostics_explicit() => _failure!.Message.ShouldContain("[diagnostics unavailable]");
    [Fact] void should_not_expose_diagnostics_transport_errors() => _failure!.Message.ShouldNotContain("secret transport");
    [Fact] void should_cancel_and_join_the_in_flight_reset() => _cancelled.ShouldBeTrue();
    [Fact] void should_retain_the_exit_over_the_rpc_cleanup_error() => _next.ShouldEqual(_failure);
    [Fact] void should_not_start_another_reset() => _resets.ShouldEqual(1);
    [Fact] void should_not_retry() => _retries.ShouldEqual(0);
}
