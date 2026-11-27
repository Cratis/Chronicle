// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsWaitHelpers;

public class when_a_terminal_failure_is_observed : Specification
{
    readonly IJobs _jobs = Substitute.For<IJobs>();
    readonly IJobs _cleanup = Substitute.For<IJobs>();
    readonly IEventStore _store = Substitute.For<IEventStore>();
    readonly Dictionary<JobStatus, JobFailedWhileWaiting> _failures = [];
    readonly JobStatus[] _terminalStatuses = [JobStatus.CompletedWithFailures, JobStatus.Failed];
    Job[] _reported = [];
    IEnumerable<Job> _allowed = [];

    void Establish()
    {
        _reported = _terminalStatuses.Select(status => new Job(_store) { Id = Guid.NewGuid(), Status = status }).ToArray();
        var running = new Job(_store) { Status = JobStatus.Running };
        _jobs.GetJobs().Returns(new[] { running, _reported[0] }, new[] { running, _reported[1] }, Array.Empty<Job>());
        _cleanup.GetJobs().Returns(_reported);
    }

    async Task Because()
    {
        foreach (var status in _terminalStatuses)
        {
            try
            {
                await _jobs.WaitForJobsToDrainSuccessfully();
            }
            catch (JobFailedWhileWaiting error)
            {
                _failures.Add(status, error);
            }
        }

        _allowed = await _cleanup.WaitForThereToBeNoJobs(includeStatuses: _terminalStatuses);
    }

    [Theory]
    [InlineData(JobStatus.CompletedWithFailures)]
    [InlineData(JobStatus.Failed)]
    void should_preserve_the_job_from_the_first_poll(JobStatus status) => _failures[status].Job.ShouldBeSame(_reported.Single(_ => _.Status == status));

    [Fact] void should_not_poll_again_after_a_terminal_failure() => _jobs.Received(2).GetJobs();
    [Fact] void should_still_allow_explicitly_included_failures_in_the_cleanup_wait() => _allowed.Count().ShouldEqual(2);
}
