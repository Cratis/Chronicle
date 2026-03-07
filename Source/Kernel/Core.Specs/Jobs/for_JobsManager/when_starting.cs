// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;
namespace Cratis.Chronicle.Jobs.for_JobsManager;

public class when_starting : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;
    SomeJobRequest _request;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _request = new(42);
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Start(It.IsAny<SomeJobRequest>())).Returns(Task.FromResult(Result.Success<StartJobError>()));
    }

    Task Because() => _manager.Start<INullJobWithSomeRequest, SomeJobRequest>(_jobId, _request);

    [Fact] void should_not_get_job_from_storage() => _jobStorage.DidNotReceive().GetJob(Arg.Any<JobId>());
    [Fact] void should_start_the_job() => _job.Verify(_ => _.Start(_request), Times.Once);
}
