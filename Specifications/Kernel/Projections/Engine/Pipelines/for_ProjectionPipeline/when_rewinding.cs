// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_rewinding : given.a_pipeline
    {
        List<IProjectionPipelineJob> started_jobs;
        Mock<IProjectionPipelineJob> job;

        async Task Establish()
        {
            started_jobs = new();
            job = new();
            pipeline.Jobs.Added.Subscribe(_ => started_jobs.Add(_));
            jobs.Setup(_ => _.Rewind(pipeline)).Returns(new[] { job.Object });
            await pipeline.Start();
            states.Clear();
        }

        async Task Because() => await pipeline.Rewind();

        [Fact] void should_have_been_through_rewinding_and_active_state() => states.ShouldContainOnly(ProjectionState.Rewinding, ProjectionState.Active);
        [Fact] void should_add_rewind_job() => started_jobs.ShouldContainOnly(job.Object);
        [Fact] void should_start_rewind_job() => job.Verify(_ => _.Run(), Once());
        [Fact] void should_not_have_active_jobs_after_all_done() => pipeline.Jobs.ShouldBeEmpty();
    }
}
