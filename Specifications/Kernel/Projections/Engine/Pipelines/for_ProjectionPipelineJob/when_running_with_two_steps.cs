// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipelineJob
{
    public class when_running_with_two_steps : Specification
    {
        ProjectionPipelineJob job;
        Mock<IProjectionPipelineJobStep> first_step;
        Mock<IProjectionPipelineJobStep> second_step;

        List<string> calls;
        string[] expected_calls = new[] { "First_Perform", "Second_Perform", "First_PerformPostJob", "Second_PerformPostJob" };

        void Establish()
        {
            first_step = new();
            second_step = new();
            job = new("Job", new[] { first_step.Object, second_step.Object });
            calls = new();
            first_step.Setup(_ => _.Perform(job.Status)).Returns((ProjectionPipelineJobStatus _) =>
            {
                calls.Add(expected_calls[0]);
                return Task.CompletedTask;
            });
            second_step.Setup(_ => _.Perform(job.Status)).Returns((ProjectionPipelineJobStatus _) =>
            {
                calls.Add(expected_calls[1]);
                return Task.CompletedTask;
            });
            first_step.Setup(_ => _.PerformPostJob(job.Status)).Returns((ProjectionPipelineJobStatus _) =>
            {
                calls.Add(expected_calls[2]);
                return Task.CompletedTask;
            });
            second_step.Setup(_ => _.PerformPostJob(job.Status)).Returns((ProjectionPipelineJobStatus _) =>
            {
                calls.Add(expected_calls[3]);
                return Task.CompletedTask;
            });
        }

        async Task Because() => await job.Run();

        [Fact] void should_call_in_expected_order() => calls.ShouldEqual(expected_calls);
    }
}
