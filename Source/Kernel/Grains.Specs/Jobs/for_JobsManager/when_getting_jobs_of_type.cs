using Cratis.Chronicle.Storage.Jobs;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_getting_jobs_of_type : given.the_manager
{
    Task Because() => _manager.GetJobsOfType<INullJobWithSomeRequest, SomeJobRequest>();

    [Fact] void should_call_job_storage() => _jobStorage.Received(1).GetJobs<INullJobWithSomeRequest, JobState>();
}