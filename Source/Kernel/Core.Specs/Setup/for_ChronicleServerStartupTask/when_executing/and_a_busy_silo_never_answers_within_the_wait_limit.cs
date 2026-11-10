// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A grain that never answers - a deadlock, or storage that hangs - times out against this silo exactly like a silo
/// busy catching up. Waiting for it is bounded, so a silo that will never get there fails instead of sitting not ready
/// forever while liveness stays green.
/// </summary>
public class and_a_busy_silo_never_answers_within_the_wait_limit : given.a_startup_task
{
    static readonly TimeSpan _limit = TimeSpan.FromMinutes(10);

    Exception _error;
    int _calls;

    TimeSpan Waited => RequestedDelays.Aggregate(TimeSpan.Zero, (total, delay) => total + delay);

    protected override ChronicleOptions ServerOptions => new() { Startup = new() { BusySiloWaitLimit = _limit } };

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo("10.0.0.1:11111:42", true));
        _eventStoreSubscriptionsManager.Ensure().Returns(_ =>
        {
            _calls++;
            return Task.FromException(new TimeoutException("Response did not arrive on time"));
        });
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail_the_host() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_report_it_as_critical() => LogEntries.Single(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.StartupStepGaveUpWaitingForBusySilo)).Level.ShouldEqual(LogLevel.Critical);
    [Fact] void should_wait_up_to_the_limit() => (Waited >= _limit).ShouldBeTrue();
    [Fact] void should_not_wait_a_whole_backoff_past_the_limit() => (Waited < _limit + TimeSpan.FromMinutes(1)).ShouldBeTrue();
    [Fact] void should_stop_calling() => _calls.ShouldEqual(RequestedDelays.Count + 1);
    [Fact] void should_not_go_on_to_later_steps() => _readModelsManager.DidNotReceive().Ensure();
}
