// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_checking_if_a_job_was_ever_prepared;

public class and_it_was_stopped_while_preparing : Specification
{
    bool _result;

    void Because() => _result = new JobState
    {
        Status = JobStatus.Stopped,
        StatusChanges =
        [
            new JobStatusChanged { Status = JobStatus.PreparingJob, Occurred = DateTimeOffset.UtcNow },
            new JobStatusChanged { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow }
        ]
    }.WasEverPrepared();

    [Fact] void should_not_have_been_prepared() => _result.ShouldBeFalse();
}
