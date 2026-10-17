// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_quarantine_begins_during_schema_resolution : given.an_observer_quarantined_during_a_probe
{
    readonly TaskCompletionSource _resolving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IEnumerable<EventTypeSchema>> _schemas = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
    {
        _resolving.TrySetResult();
        return _schemas.Task;
    });

    async Task Because()
    {
        var reconciliation = _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);
        await _resolving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.FailedPartitionRecovered(_partition, 42UL);
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();
        _jobsManager.ClearReceivedCalls();
        _schemas.TrySetResult([]);
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_preserve_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_replace_the_subscription() => (await _observer.GetSubscription()).ShouldBeSame(subscription);
    [Fact] void should_not_reload_progress() => _storageStats.Reads.ShouldEqual(0);
    [Fact] void should_not_start_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
