// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_retry_reminder_arrives_after_the_deferral_deadline : given.a_webhook_observer
{
    FailedPartition _failure;
    PartitionDeferral _deferral;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _deferral);
    }

    Task Because() => _observer.ReceiveReminder(Observer.PartitionReminderName(_failure.Id), default);

    [Fact] void should_resume_from_the_first_unhandled_event() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.FromSequenceNumber == _deferral.ResumeFrom && _.EpisodeId == _failure.Id && _.DeferralId == _deferral.Id));
    [Fact] void should_not_consume_an_attempt_just_for_waking_up() => _failure.Attempts.ShouldBeEmpty();
}
