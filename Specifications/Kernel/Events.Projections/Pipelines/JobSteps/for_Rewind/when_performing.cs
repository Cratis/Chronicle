// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines.JobSteps.for_Rewind
{
    public class when_performing : given.a_rewind_step
    {
        async Task Because() => await rewind.Perform(job_status);

        [Fact] void should_begin_rewind_for_result_store() => result_store.Verify(_ => _.BeginRewind(), Once());
        [Fact] void should_reset_projection_position() => positions.Verify(_ => _.Reset(projection.Object, configuration), Once());
    }
}
