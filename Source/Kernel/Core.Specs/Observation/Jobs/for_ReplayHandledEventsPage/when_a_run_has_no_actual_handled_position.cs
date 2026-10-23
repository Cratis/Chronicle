// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayHandledEventsPage;

public class when_a_run_has_no_actual_handled_position : Specification
{
    readonly ReplayHandledEventsPage _page = new();

    void Because()
    {
        var step = JobStepId.New();
        _page.Add(new("first", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL }, step, 3UL));
        _page.Add(new("first", new Dictionary<EventTypeId, EventCount>(), step, EventSequenceNumber.Unavailable));
        _page.Add(new("second", new Dictionary<EventTypeId, EventCount>(), step, EventSequenceNumber.Unavailable));
        _page.Add(new("third", new Dictionary<EventTypeId, EventCount>(), step, EventSequenceNumber.Max));
    }

    [Fact] void should_keep_the_actual_page_tail() => _page.LastHandled.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_actual_partition_tail() => _page.Reports.Single(r => r.Partition == (Key)"first").HandledUpTo.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_not_add_partitions_without_an_actual_position() => _page.Reports.Count.ShouldEqual(1);
    [Fact] void should_keep_the_handled_counts() => _page.Reports.Single().Counts[(EventTypeId)"event"].ShouldEqual((EventCount)1UL);
}
