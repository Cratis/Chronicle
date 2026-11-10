// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

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
    [Fact] void should_report_that_it_waits_for_this_silo() => LogMessages.Any(_ => _.Contains($"calling this silo itself ({ThisSilo})", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_escalate_a_long_wait() => LogMessages.Any(_ => _.Contains("is still timing out calling this silo itself", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_blame_a_sibling() => LogMessages.Any(_ => _.Contains("calling sibling silo", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_report_a_failed_attempt() => LogMessages.Any(_ => _.Contains("failed on attempt", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_back_off() => RequestedDelays.Take(3).ShouldContainOnly([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)]);
    [Fact] void should_cap_the_backoff() => RequestedDelays.Max().ShouldEqual(TimeSpan.FromMinutes(1));
}
