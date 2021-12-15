// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_rewinding_and_already_rewinding : given.a_pipeline
    {
        List<IProjectionPipelineJob> started_jobs;
        Mock<IProjectionPipelineJob> job;
        Exception exception;
        TaskCompletionSource tcs;

        void Establish()
        {
            tcs = new();
            started_jobs = new();
            job = new();
            pipeline.Jobs.Added.Subscribe(_ => started_jobs.Add(_));
            pipeline.Start();
            states.Clear();
            job.Setup(_ => _.Run()).Returns(tcs.Task);
            job.SetupGet(_ => _.Name).Returns(ProjectionPipelineJobs.RewindJob);
            jobs.Setup(_ => _.Rewind(pipeline)).Returns(new[] { job.Object });
            pipeline.Rewind();
        }

        void Destroy() => tcs.SetResult();

        async Task Because() => exception = await Catch.Exception(async () => await pipeline.Rewind());

        [Fact] void should_throw_rewind_already_in_progress() => exception.ShouldBeOfExactType<RewindAlreadyInProgress>();
    }
}
