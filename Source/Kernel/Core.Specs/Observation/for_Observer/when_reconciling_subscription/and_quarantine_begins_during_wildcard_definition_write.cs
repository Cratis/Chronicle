// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_quarantine_begins_during_wildcard_definition_write : given.an_observer_with_blocked_definition_write
{
    async Task Because()
    {
        var reconciliation = _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [], SiloAddress.Zero, true);
        await _definitionWrite.Writing.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.FailedPartitionRecovered(_partition, 42UL);
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();
        _jobsManager.ClearReceivedCalls();
        _definitionWrite.Release.TrySetResult();
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_preserve_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_replace_the_subscription() => (await _observer.GetSubscription()).ShouldBeSame(subscription);
    [Fact] void should_not_start_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
