// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_Verdict;

/// <summary>
/// Specifies the durability oracle without a kernel or database.
/// </summary>
public class when_comparing_snapshots : given.a_snapshot
{
    [Fact] void should_accept_a_clean_snapshot() => Verdict.Compare(Input).IsClean.ShouldBeTrue();
    [Fact] void should_report_a_missing_acknowledged_event() => Verdict.Compare(Input with { Events = [] }).Lost.ShouldEqual(1);
    [Fact] void should_report_a_duplicate_ledger_id() => Verdict.Compare(Input with { Events = [Input.Events[0], Input.Events[0]] }).Duplicated.ShouldEqual(1);
    [Fact] void should_allow_an_absent_unknown_intention() => Verdict.Compare(Input with { Ledger = [Input.Ledger[0], new("unknown", "source", 1, "unknown")] }).IsClean.ShouldBeTrue();

    [Fact]
    void should_allow_an_unknown_event_present_once()
    {
        var input = Input with
        {
            Ledger = [Input.Ledger[0], new("unknown", "source", 1, "unknown")],
            Events = [Input.Events[0], new("unknown", "source", 1, 1)],
            SideLedger = ["one", "unknown"],
            Observers = [new("observer", 1, true)],
            Projections = [new("source", 2, 8)],
            Reductions = [new("source", 2, 8)]
        };
        Verdict.Compare(input).IsClean.ShouldBeTrue();
    }

    [Fact] void should_report_unknown_present_twice() => Verdict.Compare(Input with { Ledger = [new("one", "source", 7, "unknown")], Events = [Input.Events[0], Input.Events[0]] }).Duplicated.ShouldEqual(1);
    [Fact] void should_report_a_sequence_gap() => Verdict.Compare(Input with { Events = [Input.Events[0] with { Sequence = 2 }] }).Gaps.ShouldEqual(1);
    [Fact] void should_report_an_observer_behind() => Verdict.Compare(Input with { Events = [Input.Events[0] with { Sequence = 1 }] }).ObserversBehind.ShouldEqual(1);
    [Fact] void should_report_a_missing_observer() => Verdict.Compare(Input with { Observers = [] }).ObserversBehind.ShouldEqual(1);
    [Fact] void should_report_failed_partitions() => Verdict.Compare(Input with { FailedPartitions = 1 }).FailedPartitions.ShouldEqual(1);
    [Fact] void should_report_an_incorrect_projection_count() => Verdict.Compare(Input with { Projections = [new("source", 2, 7)] }).ProjectionsBehind.ShouldEqual(1);
    [Fact] void should_report_an_incorrect_projection_sum() => Verdict.Compare(Input with { Projections = [new("source", 1, 9)] }).ProjectionsBehind.ShouldEqual(1);
    [Fact] void should_report_an_incorrect_reducer() => Verdict.Compare(Input with { Reductions = [] }).ReducersBehind.ShouldEqual(1);
    [Fact] void should_report_a_reactor_duplicate() => Verdict.Compare(Input with { SideLedger = ["one", "one"] }).ReactorDuplicates.ShouldEqual(1);
    [Fact] void should_reject_in_flight_partitions() => Verdict.Compare(Input with { InFlightPartitions = 1 }).IsClean.ShouldBeFalse();
    [Fact] void should_reject_pending_jobs() => Verdict.Compare(Input with { PendingJobs = 1 }).IsClean.ShouldBeFalse();
    [Fact] void should_reject_an_unstable_drain_without_inventing_job_counts() => Verdict.Compare(Input with { Settled = false }).IsClean.ShouldBeFalse();
    [Fact] void should_reject_missing_kernel_state() => Verdict.Compare(Input with { ExpectedStateDocuments = 0 }).IsClean.ShouldBeFalse();
    [Fact] void should_reject_a_missing_side_effect() => Verdict.Compare(Input with { SideLedger = [] }).IsClean.ShouldBeFalse();
    [Fact] void should_reject_a_payload_different_from_the_intention() => Verdict.Compare(Input with { Events = [Input.Events[0] with { Payload = 8 }] }).IsClean.ShouldBeFalse();
}
