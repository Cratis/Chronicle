// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;
namespace Cratis.Chronicle.Jobs.for_JobsManager;

public class when_resuming : given.the_manager
{
    JobId _jobId;
    Mock<INullJobWithSomeRequest> _job;

    void Establish()
    {
        _jobId = Guid.Parse("24ff9a76-a590-49b7-847d-28fcc9bf1024");
        _job = AddJob<INullJobWithSomeRequest>(_jobId);
        _job.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.Success));
    }

    Task Because() => _manager.Resume(_jobId);

    [Fact] void should_get_job_from_storage() => _jobStorage.Received(1).GetJob(_jobId);
    [Fact] void should_resume_the_job() => _job.Verify(_ => _.Resume(), Times.Once);
}
