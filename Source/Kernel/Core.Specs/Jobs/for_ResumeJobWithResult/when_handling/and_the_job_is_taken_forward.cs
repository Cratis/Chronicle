// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs.for_JobCommandOutcomes.given;

namespace Cratis.Chronicle.Jobs.for_ResumeJobWithResult.when_handling;

public class and_the_job_is_taken_forward : a_job_command
{
    ResumeJobOutcome _result;

    void Establish() => _jobsManager.Resume(_jobId).Returns(true);

    async Task Because() => _result = await new ResumeJobWithResult("some-event-store", "some-namespace", _jobId).Handle(_grainFactory);

    [Fact] void should_report_it_accepted() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_give_a_refusal() => _result.Refusal.ShouldEqual(JobCommandRefusal.NotRefused);
}
