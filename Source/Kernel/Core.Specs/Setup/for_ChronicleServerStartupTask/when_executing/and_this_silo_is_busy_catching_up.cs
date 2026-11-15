// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A single silo restarted into a backlog answers its own calls late for longer than the whole attempt budget. It is
/// busy, not broken: terminating it restarts the same backlog, so the step waits for it instead (#5676).
/// </summary>
public class and_this_silo_is_busy_catching_up : given.a_startup_task
{
    const int TimesOut = 60;
    const string ThisSilo = "10.0.0.1:11111:42";

    Exception _error;
    int _calls;

    static readonly string[] _waitsForThisSilo =
    [
        nameof(ChronicleServerStartupTaskLogging.StartupStepWaitingForThisSilo),
        nameof(ChronicleServerStartupTaskLogging.StartupStepStillWaitingForThisSilo)
    ];

    static readonly string[] _waitsForASibling =
    [
        nameof(ChronicleServerStartupTaskLogging.StartupStepWaitingForBusySibling),
        nameof(ChronicleServerStartupTaskLogging.StartupStepStillWaitingForBusySibling)
    ];

    given.StartupLogEntry[] Waits => [.. LogEntries.Where(_ => _.Event is { } name && _waitsForThisSilo.Contains(name))];

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo(ThisSilo, true));
        _eventStoreSubscriptionsManager.Ensure().Returns(_ => ++_calls <= TimesOut
            ? Task.FromException(new TimeoutException("Response did not arrive on time"))
            : Task.CompletedTask);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] void should_keep_trying_beyond_the_attempt_budget() => _calls.ShouldEqual(TimesOut + 1);
    [Fact] void should_report_every_wait() => Waits.Length.ShouldEqual(TimesOut);
    [Fact] void should_report_that_it_waits_for_this_silo() => LogMessages.Any(_ => _.Contains($"calling this silo itself ({ThisSilo})", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_at_first() => Waits.Take(10).ShouldEachConformTo(_ => _.Level == LogLevel.Warning && _.Event == nameof(ChronicleServerStartupTaskLogging.StartupStepWaitingForThisSilo));
    [Fact] void should_escalate_a_long_wait() => Waits.Skip(10).ShouldEachConformTo(_ => _.Level == LogLevel.Error && _.Event == nameof(ChronicleServerStartupTaskLogging.StartupStepStillWaitingForThisSilo));
    [Fact] void should_attach_the_timeout_to_the_first_wait() => Waits[0].Exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_attach_the_timeout_to_the_escalation() => Waits[10].Exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_repeat_the_timeout_on_other_waits() => Waits.Where((_, index) => index is not 0 and not 10).ShouldEachConformTo(_ => _.Exception == null);
    [Fact] void should_not_blame_a_sibling() => LogEntries.Any(_ => _.Event is { } name && _waitsForASibling.Contains(name)).ShouldBeFalse();
    [Fact] void should_not_report_a_failed_attempt() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.RetryingStartupStep)).ShouldBeFalse();
    [Fact] void should_back_off() => RequestedDelays.Take(3).ShouldContainOnly([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)]);
    [Fact] void should_cap_the_backoff() => RequestedDelays.Max().ShouldEqual(TimeSpan.FromMinutes(1));
}
