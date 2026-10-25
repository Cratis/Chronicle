// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Once a retry job comes out of a reminder the partition is moving again, so the next time none does the backoff
/// starts over.
/// </summary>
public class when_a_retry_job_starts_after_reminders_came_out_without_one : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    FailedPartition _failedPartition;
    List<TimeSpan> _delays;

    void Establish()
    {
        _failedPartition = new() { Partition = (Key)Partition };
        _failedPartitionsState.Partitions = [_failedPartition];
        var failed = Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown));
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(failed, failed, Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())), failed);
    }

    async Task Because()
    {
        for (var i = 0; i < 4; i++)
        {
            await _observer.ReceiveReminder(Partition, default);
        }

        _delays = RegisteredDelays();
    }

    List<TimeSpan> RegisteredDelays() => _silo.ReminderRegistry.Mock.Invocations
        .Where(_ => _.Method.Name == "RegisterOrUpdateReminder" && (string)_.Arguments[1] == Observer.PartitionReminderName(_failedPartition.Id))
        .Select(_ => (TimeSpan)_.Arguments[2])
        .ToList();

    [Fact] void should_start_the_backoff_over() => _delays.SequenceEqual([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2)]).ShouldBeTrue();
}
