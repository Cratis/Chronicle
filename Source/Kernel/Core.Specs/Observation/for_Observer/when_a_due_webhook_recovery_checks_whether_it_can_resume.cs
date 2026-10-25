// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_due_webhook_recovery_checks_whether_it_can_resume : given.a_webhook_observer
{
    FailedPartition _failure;
    PartitionDeferral _deferral;
    bool _result;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _deferral);
    }

    async Task Because() => _result = await _observer.CanResumeFailedPartition("partition", _failure.Id, _deferral.Id);

    [Fact] void should_be_allowed_to_resume() => _result.ShouldBeTrue();
    [Fact] void should_not_arm_a_reminder_that_would_start_another_recovery() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failure.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}
