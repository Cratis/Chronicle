// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Pipelines.JobSteps;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_rewinding_configuration_already_rewinding : given.a_pipeline_with_one_store
    {
        List<IProjectionPipelineJob> started_jobs;
        Mock<IProjectionPipelineJob> job;
        Rewind rewind_step;
        Exception exception;
        TaskCompletionSource tcs;
        Mock<IProjectionPositions> positions;

        void Establish()
        {
            tcs = new();
            started_jobs = new();
            job = new();
            positions = new();
            rewind_step = new(pipeline, positions.Object, configuration, Mock.Of<ILogger<Rewind>>());
            pipeline.Jobs.Added.Subscribe(_ => started_jobs.Add(_));
            pipeline.Start();
            states.Clear();
            job.Setup(_ => _.Run()).Returns(tcs.Task);
            job.SetupGet(_ => _.Name).Returns(ProjectionPipelineJobs.RewindJob);
            job.SetupGet(_ => _.Steps).Returns(new[] { rewind_step });
            jobs.Setup(_ => _.Rewind(pipeline, configuration)).Returns(job.Object);
            pipeline.Rewind(configuration);
        }

        void Destroy() => tcs.SetResult();

        async Task Because() => exception = await Catch.Exception(async () => await pipeline.Rewind(configuration));

        [Fact] void should_throw_rewind_already_in_progress() => exception.ShouldBeOfExactType<RewindAlreadyInProgressForConfiguration>();
    }
}
