// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_quarantine_precedes_dispatch : given.an_observer_quarantined_during_a_probe
{
    async Task Establish()
    {
        await _observer.FailedPartitionRecovered(_partition, 42UL);
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();
        await _observer.Unsubscribe();
        _storageStats.ResetCounts();
        _eventTypesStorage.ClearReceivedCalls();
    }

    Task Because() => _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);

    [Fact] async Task should_preserve_the_activation_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_preserve_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_install_a_subscription() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_reload_or_write_progress() => (_storageStats.Reads + _storageStats.Writes).ShouldEqual(0);
    [Fact] void should_not_read_schemas() => _eventTypesStorage.ReceivedCalls().ShouldBeEmpty();
}
