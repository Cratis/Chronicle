// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayHandledEventsPage;

public class when_the_first_run_has_no_actual_handled_position : Specification
{
    readonly ReplayHandledEventsPage _page = new();

    void Because()
    {
        var step = JobStepId.New();
        _page.Add(new("first", new Dictionary<EventTypeId, EventCount>(), step, EventSequenceNumber.Unavailable));
        _page.Add(new("first", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL }, step, 3UL));
    }

    [Fact] void should_keep_the_actual_page_tail() => _page.LastHandled.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_actual_partition_tail() => _page.Reports.Single().HandledUpTo.ShouldEqual((EventSequenceNumber)3UL);
}
