// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_cleaning_up_dead_jobs : given.the_manager
{
    JobId _oldDeadJobId;
    JobId _recentDeadJobId;
    JobId _oldJobWithStepsId;
    JobId _recentJobWithStepsId;
    Mock<INullJobWithSomeRequest> _oldDeadJob;
    Mock<INullJobWithSomeRequest> _recentDeadJob;
    Mock<INullJobWithSomeRequest> _oldJobWithSteps;
    Mock<INullJobWithSomeRequest> _recentJobWithSteps;

    void Establish()
    {
        _oldDeadJobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _recentDeadJobId = Guid.Parse("d090f5e6-5a6a-43b1-8580-4973e3c69521");
        _oldJobWithStepsId = Guid.Parse("3f8e5d3c-4b2a-4e6f-9d8c-7f5a6b8c9d0e");
        _recentJobWithStepsId = Guid.Parse("5a6b7c8d-9e0f-1a2b-3c4d-5e6f7a8b9c0d");

        _oldDeadJob = AddJob<INullJobWithSomeRequest>(_oldDeadJobId);
        _storedJobs.Single(j => j.Id == _oldDeadJobId).Status = JobStatus.PreparingSteps;
        _storedJobs.Single(j => j.Id == _oldDeadJobId).Created = DateTimeOffset.UtcNow.AddHours(-2);

        _recentDeadJob = AddJob<INullJobWithSomeRequest>(_recentDeadJobId);
        _storedJobs.Single(j => j.Id == _recentDeadJobId).Status = JobStatus.PreparingJob;
        _storedJobs.Single(j => j.Id == _recentDeadJobId).Created = DateTimeOffset.UtcNow.AddMinutes(-30);

        _oldJobWithSteps = AddJob<INullJobWithSomeRequest>(_oldJobWithStepsId);
        _storedJobs.Single(j => j.Id == _oldJobWithStepsId).Status = JobStatus.PreparingSteps;
        _storedJobs.Single(j => j.Id == _oldJobWithStepsId).Created = DateTimeOffset.UtcNow.AddHours(-2);

        _recentJobWithSteps = AddJob<INullJobWithSomeRequest>(_recentJobWithStepsId);
        _storedJobs.Single(j => j.Id == _recentJobWithStepsId).Status = JobStatus.PreparingJob;
        _storedJobs.Single(j => j.Id == _recentJobWithStepsId).Created = DateTimeOffset.UtcNow.AddMinutes(-30);

        _jobStepStorage.CountForJob(_oldJobWithStepsId, Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch.Success(5)));
        _jobStepStorage.CountForJob(_recentJobWithStepsId, Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch.Success(3)));
        _jobStepStorage.CountForJob(_oldDeadJobId, Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch.Success(0)));
        _jobStepStorage.CountForJob(_recentDeadJobId, Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch.Success(0)));

        _oldDeadJob.Setup(_ => _.Remove()).ReturnsAsync(Result.Success<RemoveJobError>());
        _recentDeadJob.Setup(_ => _.Remove()).ReturnsAsync(Result.Success<RemoveJobError>());
        _oldJobWithSteps.Setup(_ => _.Remove()).ReturnsAsync(Result.Success<RemoveJobError>());
        _recentJobWithSteps.Setup(_ => _.Remove()).ReturnsAsync(Result.Success<RemoveJobError>());
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] void should_get_jobs_that_are_preparing() => _jobStorage.Received(1).GetJobs(JobStatus.PreparingJob, JobStatus.PreparingSteps);
    [Fact] void should_remove_old_dead_job() => _oldDeadJob.Verify(_ => _.Remove(), Times.Once);
    [Fact] void should_not_remove_recent_dead_job() => _recentDeadJob.Verify(_ => _.Remove(), Times.Never);
    [Fact] void should_not_remove_old_job_with_steps() => _oldJobWithSteps.Verify(_ => _.Remove(), Times.Never);
    [Fact] void should_not_remove_recent_job_with_steps() => _recentJobWithSteps.Verify(_ => _.Remove(), Times.Never);
}
