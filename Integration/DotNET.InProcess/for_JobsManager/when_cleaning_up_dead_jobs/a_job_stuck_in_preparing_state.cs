// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;
using Cratis.Chronicle.Storage.Jobs;
using context = Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_cleaning_up_dead_jobs.a_job_stuck_in_preparing_state.context;
using JobId = Cratis.Chronicle.Concepts.Jobs.JobId;
using JobStatus = Cratis.Chronicle.Concepts.Jobs.JobStatus;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_cleaning_up_dead_jobs;

[Collection(ChronicleCollection.Name)]
public class a_job_stuck_in_preparing_state(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public JobId JobId { get; private set; }

        async Task Establish()
        {
            JobId = JobId.New();
            var deadJobState = new JobState
            {
                Id = JobId,
                Type = typeof(JobWithSingleStep),
                Status = JobStatus.PreparingJob,
                Created = DateTimeOffset.UtcNow.AddHours(-2)
            };
            await JobStorage.Save(JobId, deadJobState);
        }

        async Task Because()
        {
            await JobsManager.CleanupDeadJobs();
            await EventStore.Jobs.WaitTillJobIsDeleted(JobId.Value);
        }
    }

    [Fact]
    async Task should_have_removed_the_dead_job()
    {
        var jobs = await Context.EventStore.Jobs.GetJobs();
        jobs.ShouldNotContain(j => j.Id.Value == Context.JobId.Value);
    }
}
