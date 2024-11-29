using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_resuming_job_that_doesnt_exist : given.the_manager
{
    JobId _jobId;
    Exception _error;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.Resume(_jobId));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_get_job_from_storage() => _jobStorage.Received(1).GetJob(_jobId);
}