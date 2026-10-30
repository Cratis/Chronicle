// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// An expired deferral re-arms its wake-up after a millisecond. When no retry job comes out of it, that would be a
/// retry storm, so the next retry waits for the backoff.
/// </summary>
public class when_a_retry_reminder_arrives_after_the_deferral_deadline_and_no_retry_job_could_be_started : given.a_webhook_observer
{
    FailedPartition _failedPartition;
    List<TimeSpan> _delays;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _failedPartition = _failedPartitionsState.Defer("partition", new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now.AddMinutes(-2), now.AddMinutes(-1), new(42)));
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
    }

    async Task Because()
    {
        await _observer.ReceiveReminder(Observer.PartitionReminderName(_failedPartition.Id), default);
        _delays = RegisteredDelays();
    }

    List<TimeSpan> RegisteredDelays() => _silo.ReminderRegistry.Mock.Invocations
        .Where(_ => _.Method.Name == "RegisterOrUpdateReminder" && (string)_.Arguments[1] == Observer.PartitionReminderName(_failedPartition.Id))
        .Select(_ => (TimeSpan)_.Arguments[2])
        .ToList();

    [Fact] void should_not_re_arm_at_the_expired_deferral_delay() => (_delays[^1] >= TimeSpan.FromSeconds(2)).ShouldBeTrue();
}
