// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs.for_JobCommandOutcomes.given;

namespace Cratis.Chronicle.Jobs.for_ResumeJob.when_handling;

public class and_nothing_takes_the_job_forward : a_job_command
{
    ResumeJobOutcome _result;

    void Establish() => _jobsManager.Resume(_jobId).Returns(false);

    async Task Because() => _result = await new ResumeJob("some-event-store", "some-namespace", _jobId).Handle(_grainFactory);

    [Fact] void should_report_it_was_not_accepted() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_say_it_was_not_resumed() => _result.Refusal.ShouldEqual(JobCommandRefusal.NotResumed);
}
