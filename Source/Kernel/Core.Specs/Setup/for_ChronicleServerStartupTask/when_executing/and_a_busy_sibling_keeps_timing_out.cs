// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A silo joining a cluster whose active member is still draining a backlog sees its calls into that member time out.
/// Counting those towards the attempt budget crash-looped the joining silo, and each restart moved more grains onto the
/// busy one (#5676). The joining silo waits instead.
/// </summary>
public class and_a_busy_sibling_keeps_timing_out : given.a_startup_task
{
    const int TimesOut = 60;
    const string Sibling = "10.0.0.2:11111:7";

    Exception _error;
    int _calls;

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo(Sibling, false));
        _readModelsManager.Ensure().Returns(_ => ++_calls <= TimesOut
            ? Task.FromException(new TimeoutException("Response did not arrive on time"))
            : Task.CompletedTask);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] void should_keep_trying_beyond_the_attempt_budget() => _calls.ShouldEqual(TimesOut + 1);
    [Fact] void should_report_the_busy_sibling() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.StartupStepWaitingForBusySibling)).ShouldBeTrue();
    [Fact] void should_escalate_a_long_wait() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.StartupStepStillWaitingForBusySibling)).ShouldBeTrue();
    [Fact] void should_name_the_sibling() => LogMessages.Any(_ => _.Contains(Sibling, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_report_a_failed_attempt() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.RetryingStartupStep)).ShouldBeFalse();
}
