// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_VerdictResult;

/// <summary>
/// Specifies the repository's clean and defect exit-code contract.
/// </summary>
public class when_mapping_exit_codes : for_Verdict.given.a_snapshot
{
    [Fact] void should_exit_zero_for_a_complete_clean_snapshot() => Verdict.Compare(Input).ExitCode.ShouldEqual(0);
    [Fact] void should_exit_one_for_the_self_test_missing_event() => Verdict.Compare(Input with { Events = [] }).ExitCode.ShouldEqual(1);
    [Fact] void should_exit_two_when_only_the_stability_window_is_missing() => Verdict.Compare(Input with { Settled = false }).ExitCode.ShouldEqual(2);
    [Fact] void should_explain_an_inconclusive_stability_window() => Verdict.Compare(Input with { Settled = false }).InconclusiveReason.ShouldContain("three stable durable-position samples");
    [Fact] void should_not_invent_a_violation_for_an_unobserved_window() => Verdict.Compare(Input with { Settled = false }).Violations.Count.ShouldEqual(0);
    [Fact] void should_not_classify_an_unobserved_window_as_a_finding() => Verdict.Compare(Input with { Settled = false }).HasFindings.ShouldBeFalse();
    [Fact] void should_allow_at_least_once_redeliveries() => Verdict.Compare(Input with { SideLedger = ["one", "one"] }).ExitCode.ShouldEqual(0);
    [Fact] void should_keep_redeliveries_informational_without_the_window() => Verdict.Compare(Input with { SideLedger = ["one", "one"], Settled = false }).ExitCode.ShouldEqual(2);
    [Fact] void should_report_observer_lag_even_without_the_window() => Verdict.Compare(Input with { Observers = [], Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_failed_partitions_even_without_the_window() => Verdict.Compare(Input with { FailedPartitions = 1, Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_projection_lag_even_without_the_window() => Verdict.Compare(Input with { Projections = [], Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_reducer_lag_even_without_the_window() => Verdict.Compare(Input with { Reductions = [], Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_event_loss_even_without_the_window() => Verdict.Compare(Input with { Events = [], Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_pending_jobs_even_without_the_window() => Verdict.Compare(Input with { PendingJobs = 1, Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_in_flight_partitions_even_without_the_window() => Verdict.Compare(Input with { InFlightPartitions = 1, Settled = false }).ExitCode.ShouldEqual(1);
    [Fact] void should_report_content_violations_even_without_the_window() => Verdict.Compare(Input with { Events = [Input.Events[0] with { Payload = 8 }], Settled = false }).ExitCode.ShouldEqual(1);
}
