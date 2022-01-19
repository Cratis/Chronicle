// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Rewind
{
    public class when_performing_post_job : given.a_rewind_step
    {
        async Task Establish() => await rewind.Perform(job_status);
        async Task Because() => await rewind.PerformPostJob(job_status);

        [Fact] void should_dispose_rewind_scope() => rewind_scope.Verify(_ => _.Dispose(), Once());
    }
}
