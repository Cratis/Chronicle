// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Waiting for a busy silo does not count attempts, so shutting down is what ends it before the wait limit does.
/// </summary>
public class and_shutdown_cancels_a_busy_silo_wait : given.a_startup_task
{
    const int CallsBeforeShutdown = 3;

    readonly CancellationTokenSource _cancellation = new();
    Exception? _failure;
    int _calls;

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo("10.0.0.1:11111:42", true));
        _eventStoreSubscriptionsManager.Ensure().Returns(async _ =>
        {
            if (++_calls == CallsBeforeShutdown) await _cancellation.CancelAsync();
            throw new TimeoutException("Response did not arrive on time");
        });
    }

    async Task Because() => _failure = await Catch.Exception(() => Execute(_cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_surface_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_stop_waiting_at_shutdown() => _calls.ShouldEqual(CallsBeforeShutdown);
    [Fact] void should_not_report_a_failure() => LogEntries.Any(_ => _.Level == LogLevel.Critical).ShouldBeFalse();
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
