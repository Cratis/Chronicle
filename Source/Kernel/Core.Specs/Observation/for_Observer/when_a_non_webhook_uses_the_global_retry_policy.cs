// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_non_webhook_uses_the_global_retry_policy : given.an_observer
{
    void Establish()
    {
        for (var index = 0; index < 9; index++)
        {
            _failedPartitionsState.RegisterAttempt("partition", 42UL, ["failure"], "", FailureKind.Handling);
        }
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "", FailureKind.Handling);
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "", FailureKind.Handling);
    }

    [Fact] void should_retain_the_global_backoff_cap() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(600)), Times.Once);
    [Fact] void should_park_after_ten_retries() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_not_apply_webhook_observer_quarantine() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
}
