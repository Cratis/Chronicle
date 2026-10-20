// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_reactivating_a_deferred_partition : given.a_webhook_observer
{
    FailedPartitionId _episode;
    PartitionDeferral _deferral;

    async Task Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddHours(1), new(42));
        await _observer.PartitionDeferred("partition", _deferral);
        _episode = _failedPartitionsState.Partitions.Single().Id;
    }

    Task Because() => Crash();

    [Fact] void should_keep_the_deadline_and_cursor() => _failedPartitionsState.Partitions.Single().Deferral.ShouldEqual(_deferral);
    [Fact] void should_rearm_the_durable_wakeup() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_episode), It.Is<TimeSpan>(delay => delay > TimeSpan.FromMinutes(59)), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_not_invent_an_attempt() => _failedPartitionsState.Partitions.Single().Attempts.ShouldBeEmpty();
}
