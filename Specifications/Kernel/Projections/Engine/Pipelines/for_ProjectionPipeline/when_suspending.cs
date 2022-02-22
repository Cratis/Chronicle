// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline;

public class when_suspending : given.a_pipeline
{
    Mock<IProjectionPipelineJob> job;
    TaskCompletionSource tcs;
    void Establish()
    {
        job = new();
        tcs = new();

        job.Setup(_ => _.Run()).Returns(tcs.Task);
        jobs.Setup(_ => _.Catchup(pipeline)).Returns(new[] { job.Object });
        pipeline.Start();
        states.Clear();
    }

    void Destroy() => tcs.SetResult();

    async Task Because() => await pipeline.Suspend(string.Empty);

    [Fact] void should_have_been_through_suspended_state() => states.ShouldContainOnly(ProjectionState.Suspended);
    [Fact] void should_stop_the_running_job() => job.Verify(_ => _.Stop(), Once());
}
