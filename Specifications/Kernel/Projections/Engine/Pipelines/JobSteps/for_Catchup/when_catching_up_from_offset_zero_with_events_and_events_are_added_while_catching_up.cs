// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup;

public class when_catching_up_from_offset_zero_with_events_and_events_are_added_while_catching_up : given.two_cursors_in_sequence_with_ten_events_each
{
    void Establish() => positions.Setup(_ => _.GetFor(projection.Object, configuration)).Returns(Task.FromResult<EventLogSequenceNumber>(0));

    async Task Because() => await catchup.Perform(job_status);

    [Fact] void should_handle_all_events_for_first_cursor() => handler.Verify(_ => _.Handle(IsIn(first_cursor_events), pipeline.Object, result_store.Object, configuration), Exactly(first_cursor_events.Count()));
    [Fact] void should_handle_all_events_for_second_cursor() => handler.Verify(_ => _.Handle(IsIn(second_cursor_events), pipeline.Object, result_store.Object, configuration), Exactly(second_cursor_events.Count()));
}
