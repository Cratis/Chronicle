// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_operator_clearance_still_owes_recovery : given.an_observer_with_failed_clearance_recovery
{
    Guid _lifecycle;
    async Task Establish()
    {
        _observer.OwesRecoveryAfterQuarantine().ShouldBeTrue();
        _lifecycle = (await _observer.GetState()).AlertLifecycleId;
        _storageStats.ResetCounts();
        _eventTypesStorage.ClearReceivedCalls();
    }

    Task Because() => _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);

    [Fact] void should_preserve_the_owed_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeTrue();
    [Fact] async Task should_preserve_the_lifecycle() => (await _observer.GetState()).AlertLifecycleId.ShouldEqual(_lifecycle);
    [Fact] void should_not_reload_or_write_progress() => (_storageStats.Reads + _storageStats.Writes).ShouldEqual(0);
    [Fact] void should_not_read_schemas() => _eventTypesStorage.ReceivedCalls().ShouldBeEmpty();
}
