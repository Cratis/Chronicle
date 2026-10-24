// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_coalescing_a_webhook_backoff : given.a_webhook_observer
{
    bool _wakeupArmedBeforeSaving;

    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 1000 };

    void Establish() => _eventStoreNamespaceStorage.FailedPartitions.When(storage => storage.Save(_observerId, Arg.Any<FailedPartitions>())).Do(_ =>
    {
        var failure = _failedPartitionsState.Partitions.Single();
        _wakeupArmedBeforeSaving = _silo.ReminderRegistry.Mock.Invocations.Any(call => call.Method.Name == "RegisterOrUpdateReminder" && Equals(call.Arguments[1], Observer.PartitionReminderName(failure.Id)));
    });

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling);

    [Fact] void should_make_the_saved_deadline_recoverable() => _wakeupArmedBeforeSaving.ShouldBeTrue();
    [Fact] void should_persist_the_selected_deadline() => _eventStoreNamespaceStorage.FailedPartitions.Received(1).Save(_observerId, Arg.Is<FailedPartitions>(failures => failures.Partitions.Single().Deferral != null));
}
