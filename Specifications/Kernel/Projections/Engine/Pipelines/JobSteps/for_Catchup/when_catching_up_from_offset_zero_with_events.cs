// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup
{
    public class when_catching_up_from_offset_zero_with_events : given.ten_events
    {
        void Establish() => positions.Setup(_ => _.GetFor(projection.Object, configuration)).Returns(Task.FromResult<EventLogSequenceNumber>(0));

        async Task Because() => await catchup.Perform(job_status);

        [Fact] void should_handle_all_events() => handler.Verify(_ => _.Handle(IsIn(events), pipeline.Object, result_store.Object, configuration), Exactly(events.Count()));
    }
}
