// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_retry_reminder_arrives_before_the_deferral_deadline : given.a_webhook_observer
{
    FailedPartition _failure;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _failure = _failedPartitionsState.Defer("partition", new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now, now.AddHours(1), new(42)));
    }

    Task Because() => _observer.ReceiveReminder(Observer.PartitionReminderName(_failure.Id), default);

    [Fact] void should_not_start_a_delivery() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_not_consume_an_attempt() => _failure.Attempts.ShouldBeEmpty();
    [Fact] void should_keep_the_deferral() => _failure.Deferral.ShouldNotBeNull();
}
