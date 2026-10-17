// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs.for_JobCommandOutcomes.given;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs.for_StopJob.when_handling;

public class and_the_job_is_already_completed : a_job_command
{
    StopJobOutcome _result;

    void Establish() => _jobsManager.Stop(_jobId).Returns(Result<StopJobError>.Failed(StopJobError.JobIsCompleted));

    async Task Because() => _result = await new StopJob("some-event-store", "some-namespace", _jobId).Handle(_grainFactory);

    [Fact] void should_report_it_was_not_accepted() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_say_the_job_is_completed() => _result.Refusal.ShouldEqual(JobCommandRefusal.JobIsCompleted);
}
