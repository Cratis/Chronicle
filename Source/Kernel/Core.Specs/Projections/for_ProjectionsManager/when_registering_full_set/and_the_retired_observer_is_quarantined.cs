// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_the_retired_observer_is_quarantined : given.a_projections_manager_grain
{
    void Establish()
    {
        _state.Projections = [CreateDefinition("orphaned-projection", "orphaned-read-model")];
        _observerGrain.IsObserverQuarantined().Returns(true);
    }

    async Task Because() => await _grain.Register([], ProjectionOwner.Client);

    [Fact] async Task should_clear_the_failed_partitions_through_the_observer() => await _observerGrain.Received(1).ClearFailedPartitions();
    [Fact] async Task should_clear_the_observers_quarantine() => await _observerGrain.Received(1).ClearObserverQuarantine();
    [Fact] async Task should_report_the_retired_observers_alerts_as_removed() => await _observerAlerts.Received(1).Removed();
    [Fact] async Task should_not_remove_the_observer() => await _observerGrain.DidNotReceive().Remove();
    [Fact]
    void should_report_removed_only_after_the_observers_failure_state_is_cleared() => Received.InOrder(() =>
    {
        _observerGrain.Unsubscribe();
        _observerGrain.ClearFailedPartitions();
        _observerGrain.IsObserverQuarantined();
        _observerGrain.ClearObserverQuarantine();
        _jobsManager.GetAllJobs();
        _observerAlerts.Removed();
    });
}
