// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// When no retry job comes out of the reminders, re-arming at the normal delay for ever is a retry storm. The delay
/// doubles from 2 seconds and is capped.
/// </summary>
public class when_reminders_keep_coming_out_without_a_retry_job : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    FailedPartition _failedPartition;
    List<TimeSpan> _delays;

    void Establish()
    {
        _failedPartition = new() { Partition = (Key)Partition };
        _failedPartitionsState.Partitions = [_failedPartition];
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
    }

    async Task Because()
    {
        for (var i = 0; i < 10; i++)
        {
            await _observer.ReceiveReminder(Partition, default);
        }

        _delays = RegisteredDelays();
    }

    List<TimeSpan> RegisteredDelays() => _silo.ReminderRegistry.Mock.Invocations
        .Where(_ => _.Method.Name == "RegisterOrUpdateReminder" && (string)_.Arguments[1] == Observer.PartitionReminderName(_failedPartition.Id))
        .Select(_ => (TimeSpan)_.Arguments[2])
        .ToList();

    [Fact] void should_double_the_delay_each_time() => _delays.Take(4).SequenceEqual([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)]).ShouldBeTrue();
    [Fact] void should_cap_the_delay() => _delays[^1].ShouldEqual(Observer.MaxRetryWithoutJobDelay);
    [Fact] void should_never_exceed_the_cap() => _delays.ShouldEachConformTo(_ => _ <= Observer.MaxRetryWithoutJobDelay);
}
