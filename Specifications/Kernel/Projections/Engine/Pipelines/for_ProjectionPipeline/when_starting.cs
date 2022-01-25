// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_starting : given.a_pipeline
    {
        List<IProjectionPipelineJob> started_jobs;
        Mock<IProjectionPipelineJob> job;

        void Establish()
        {
            started_jobs = new();
            job = new();
            pipeline.Jobs.Added.Subscribe(_ => started_jobs.Add(_));
            jobs.Setup(_ => _.Catchup(pipeline)).Returns(new[] { job.Object });
        }

        async Task Because() => await pipeline.Start();

        [Fact] void should_have_been_through_registering_catchup_and_active_state() => states.ShouldContainOnly(ProjectionState.Registering, ProjectionState.CatchingUp, ProjectionState.Active);
        [Fact] void should_add_catchup_job() => started_jobs.ShouldContainOnly(job.Object);
        [Fact] void should_start_catchup_job() => job.Verify(_ => _.Run(), Once());
        [Fact] void should_not_have_active_jobs_after_all_done() => pipeline.Jobs.ShouldBeEmpty();
    }
}
