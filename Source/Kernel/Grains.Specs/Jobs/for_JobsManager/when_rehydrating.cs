using Cratis.Chronicle.Concepts.Jobs;
using Moq;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_rehydrating : given.the_manager
{
    JobId _firstJobId;
    JobId _secondJobId;
    Mock<INullJobWithSomeRequest> _firstJob;
    Mock<INullJobWithSomeRequest> _secondJob;

    void Establish()
    {
        _firstJobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _secondJobId = Guid.Parse("d090f5e6-5a6a-43b1-8580-4973e3c69521");
        _firstJob = AddJob<INullJobWithSomeRequest>(_firstJobId);
        _secondJob = AddJob<INullJobWithSomeRequest>(_secondJobId);
    }

    Task Because() => _manager.Rehydrate();

    [Fact] void should_get_jobs_that_are_running_preparing_or_preparing_steps() => _jobStorage.Received(1).GetJobs(JobStatus.Running, JobStatus.PreparingStepsForRunning, JobStatus.PreparingSteps);
    [Fact] void should_resume_first_job() => _firstJob.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_resume_second_job() => _secondJob.Verify(_ => _.Resume(), Times.Once);
}