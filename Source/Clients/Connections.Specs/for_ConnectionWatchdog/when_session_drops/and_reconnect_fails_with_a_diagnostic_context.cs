// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_reconnect_fails_with_a_diagnostic_context : given.a_connection_watchdog
{
    const string FirstContext = "endpoint: node-a:35000, phase: awaiting the handshake";
    const string SecondContext = "no endpoint attempted yet, phase: resolving servers";

    ConnectionWatchdog _contextual;
    for_ChronicleConnection.given.a_logger_that_remembers_messages<ConnectionWatchdog> _captured;
    int _failedAttempts;
    TaskCompletionSource _done;

    void Establish()
    {
        _captured = new();
        _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _contextual = new ConnectionWatchdog(
            _tasks,
            () => Task.CompletedTask,
            () =>
            {
                if (Interlocked.Increment(ref _failedAttempts) <= 3)
                {
                    throw new SimulatedFailure();
                }

                _contextual!.NotifyKeepAlive();
                _done.TrySetResult();
                return Task.CompletedTask;
            },
            _captured,
            _cancellationTokenSource.Token,
            _time)
        {
            DiagnosticContext = () => _failedAttempts switch
            {
                1 => FirstContext,
                2 => SecondContext,
                _ => throw new InvalidOperationException("The context cannot be described")
            }
        };
        _time.Advance(TimeSpan.FromSeconds(6));
    }

    async Task Because()
    {
        _contextual.Start();
        await _done.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _cancellationTokenSource.CancelAsync();
    }

    IEnumerable<string> Failures => _captured.AtLevel(LogLevel.Error).Select(_ => _.Message);

    [Fact] void should_log_each_failed_attempt() => Failures.Count().ShouldEqual(3);
    [Fact] void should_log_the_endpoint_and_phase_of_the_first_failure() => Failures.ElementAt(0).ShouldContain(FirstContext);
    [Fact] void should_log_when_no_endpoint_was_attempted() => Failures.ElementAt(1).ShouldContain(SecondContext);
    [Fact] void should_still_log_the_failure_when_the_context_cannot_be_described() => Failures.ElementAt(2).ShouldContain("no connect context available");
}
