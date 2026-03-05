// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Storage.Jobs;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Jobs.for_JobsManager;

public class when_getting_jobs_of_type : given.the_manager
{
    void Establish()
    {
        _jobStorage
            .GetJobs<INullJobWithSomeRequest, JobState>()
            .Returns(Catch.Success<IImmutableList<JobState>, Storage.Jobs.JobError>([]));
    }

    Task Because() => _manager.GetJobsOfType<INullJobWithSomeRequest, SomeJobRequest>();

    [Fact] void should_call_job_storage() => _jobStorage.Received(1).GetJobs<INullJobWithSomeRequest, JobState>();
}
