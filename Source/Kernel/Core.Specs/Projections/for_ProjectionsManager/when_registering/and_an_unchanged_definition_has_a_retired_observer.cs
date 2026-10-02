// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_an_unchanged_definition_has_a_retired_observer : given.a_manager_recovering_a_retired_observer
{
    async Task Establish() => await _managerSilo.TimerRegistry.FireAllAsync();

    async Task Because() => await _manager.Register([_projectionDefinition]);

    [Fact] async Task should_deliberately_reactivate_the_observer() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_start_a_fresh_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_retiredLifecycle);
    [Fact] void should_publish_active_state() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_discard_retired_failures() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
}
