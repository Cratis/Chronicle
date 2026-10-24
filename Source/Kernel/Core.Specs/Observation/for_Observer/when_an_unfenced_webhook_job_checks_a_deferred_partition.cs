// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_unfenced_webhook_job_checks_a_deferred_partition : given.a_webhook_observer
{
    PartitionDeferral _deferral;
    FailedPartition _failure;
    bool _canResume;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now, now.AddMinutes(1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _deferral);
    }

    async Task Because() => _canResume = await _observer.CanResumeFailedPartition("partition");

    [Fact] void should_leave_recovery_to_the_owning_episode() => _canResume.ShouldBeFalse();
    [Fact] void should_preserve_the_resume_position_and_deadline() => _failure.Deferral.ShouldEqual(_deferral);
    [Fact] void should_not_consume_an_attempt() => _failure.AttemptsInCurrentBudget.ShouldEqual(0);
}
