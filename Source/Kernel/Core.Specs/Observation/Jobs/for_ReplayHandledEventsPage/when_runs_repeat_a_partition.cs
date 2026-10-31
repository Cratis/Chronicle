// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayHandledEventsPage;

public class when_runs_repeat_a_partition : Specification
{
    readonly ReplayHandledEventsPage _page = new();

    void Because()
    {
        var step = JobStepId.New();
        _page.Add(new("module", new Dictionary<EventTypeId, EventCount> { ["a"] = 2UL, ["b"] = 1UL }, step, 1UL));
        _page.Add(new("feature", new Dictionary<EventTypeId, EventCount> { ["a"] = 1UL }, step, 2UL));
        _page.Add(new("module", new Dictionary<EventTypeId, EventCount> { ["b"] = 3UL, ["c"] = 1UL }, step, 3UL));
    }

    [Fact] void should_sum_the_matching_type() => _page.Reports.Single(r => r.Partition == (Key)"module").Counts[(EventTypeId)"b"].ShouldEqual((EventCount)4UL);
    [Fact] void should_keep_the_earlier_type() => _page.Reports.Single(r => r.Partition == (Key)"module").Counts[(EventTypeId)"a"].ShouldEqual((EventCount)2UL);
    [Fact] void should_add_the_new_type() => _page.Reports.Single(r => r.Partition == (Key)"module").Counts[(EventTypeId)"c"].ShouldEqual((EventCount)1UL);
    [Fact] void should_keep_the_page_tail() => _page.LastHandled.ShouldEqual((EventSequenceNumber)3UL);
}
