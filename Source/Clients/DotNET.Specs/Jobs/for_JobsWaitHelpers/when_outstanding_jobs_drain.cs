// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsWaitHelpers;

public class when_outstanding_jobs_drain : Specification
{
    readonly IJobs _jobs = Substitute.For<IJobs>();

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        Job[] outstanding = [new(store) { Status = JobStatus.Running }, new(store) { Status = JobStatus.Stopped }];
        _jobs.GetJobs().Returns(outstanding, Array.Empty<Job>());
    }

    Task Because() => _jobs.WaitForJobsToDrainSuccessfully();

    [Fact] void should_poll_until_the_running_and_resumable_jobs_are_gone() => _jobs.Received(2).GetJobs();
}
