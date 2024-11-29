using Cratis.Chronicle.Concepts.Jobs;
using Orleans.TestKit;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_starting_job_that_doesnt_exist : given.the_manager
{
    JobId _jobId;
    Exception _error;
    SomeJobRequest _request;
    INullJobWithSomeRequest _job;
    void Establish()
    {
        _request = new(42);
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = Substitute.For<INullJobWithSomeRequest>();
        _silo.AddProbe(_ => _job);
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.Start<INullJobWithSomeRequest, SomeJobRequest>(_jobId, _request));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_get_job_from_storage() => _jobStorage.DidNotReceive().GetJob(Arg.Any<JobId>());
    [Fact] void should_start_the_job_grain() => _job.Received(1).Start(_request);
}