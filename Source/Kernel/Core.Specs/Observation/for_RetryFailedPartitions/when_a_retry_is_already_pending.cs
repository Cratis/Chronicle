// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

/// <summary>
/// Repeating the command while its job is unfinished joins that job instead of creating another.
/// </summary>
public class when_a_retry_is_already_pending : given.a_retry_command
{
    RetryFailedPartitionsResult _result;

    void Establish()
    {
        HaveFailedPartitions(3);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(
            ImmutableList.Create(new JobState
            {
                Id = JobId.New(),
                Status = JobStatus.Running,
                Request = new ObserverOperatorCommandRequest(_key, ObserverOperatorCommand.RetryFailedPartitions)
            })));
    }

    async Task Because() => _result = await new RetryFailedPartitions("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage);

    [Fact] void should_succeed_with_the_failed_partition_count() => _result.FailedPartitions.ShouldEqual(3);
    [Fact] void should_not_create_another_job() => _jobsManager.DidNotReceive().Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
