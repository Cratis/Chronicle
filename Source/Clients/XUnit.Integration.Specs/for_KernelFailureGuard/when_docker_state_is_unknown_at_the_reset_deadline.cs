// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_docker_state_is_unknown_at_the_reset_deadline : Specification
{
    readonly IOException _original = new("reset failed");
    Exception? _failure;
    int _logs;
    int _retries;
    bool _monitorStopped;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromException<long?>(new IOException("Docker unavailable")),
            _ =>
            {
                _logs++;
                return Task.FromResult("unused");
            },
            async cancellationToken =>
            {
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = cancellationToken.Register(() => completion.SetCanceled(cancellationToken));
                try
                {
                    await completion.Task;
                }
                finally
                {
                    _monitorStopped = true;
                }
            });
        _failure = await Catch.Exception(() => guard.Run(
            _ => Task.FromException(_original),
            () => false,
            () =>
            {
                _retries++;
                return Task.CompletedTask;
            }));
    }

    [Fact] void should_not_mislabel_an_inspect_failure_as_exit() => _failure.ShouldEqual(_original);
    [Fact] void should_not_extend_the_reset_deadline() => _retries.ShouldEqual(0);
    [Fact] void should_stop_monitoring_before_teardown() => _monitorStopped.ShouldBeTrue();
    [Fact] void should_not_request_logs_for_unknown_state() => _logs.ShouldEqual(0);
}
