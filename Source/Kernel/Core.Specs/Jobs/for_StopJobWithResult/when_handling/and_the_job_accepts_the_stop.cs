// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs.for_JobCommandOutcomes.given;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs.for_StopJobWithResult.when_handling;

public class and_the_job_accepts_the_stop : a_job_command
{
    StopJobOutcome _result;

    void Establish() => _jobsManager.Stop(_jobId).Returns(Result<StopJobError>.Success());

    async Task Because() => _result = await new StopJobWithResult("some-event-store", "some-namespace", _jobId).Handle(_grainFactory);

    [Fact] void should_report_it_accepted() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_give_a_refusal() => _result.Refusal.ShouldEqual(JobCommandRefusal.NotRefused);
}
