// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_canceling_removal;

public class and_reconciliation_failed_after_persisting_the_fence : given.an_observer
{
    Guid _fencedLifecycle;

    async Task Establish()
    {
        FailAlertReports(new ObserverAlertsNotReconciled(_observerKey));
        await Catch.Exception(_observer.Remove);
        _fencedLifecycle = _stateStorage.State.AlertLifecycleId;
    }

    async Task Because() => await _observer.CancelRemoval();

    [Fact] void should_restore_the_active_disposition() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_replace_the_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_fencedLifecycle);
}
