// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_deferral_wakeup_cannot_be_registered : given.a_webhook_observer
{
    Exception _error;

    void Establish() => _silo.ReminderRegistry.Mock.Setup(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>())).ThrowsAsync(new InvalidOperationException("reminders unavailable"));

    async Task Because()
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            await _observer.PartitionDeferred("partition", new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddMinutes(1), new(42)));
        }
        catch (Exception error)
        {
            _error = error;
        }
    }

    [Fact] void should_fail_the_delivery_acknowledgement() => _error.ShouldNotBeNull();
    [Fact] void should_not_commit_an_unrecoverable_deadline() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
