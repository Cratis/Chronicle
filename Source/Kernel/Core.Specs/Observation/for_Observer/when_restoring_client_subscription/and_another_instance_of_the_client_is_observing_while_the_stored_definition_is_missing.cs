// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// A missing stored definition belongs to a wiped world, which only the full reconcile rebuilds.
/// </summary>
public class and_another_instance_of_the_client_is_observing_while_the_stored_definition_is_missing : given.an_observer_restoring_another_client_instance
{
    void Establish() => _definitionStorage.State = new ObserverDefinition { Identifier = _observerId };

    Task Because() => RestoreSecondClient();

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_write_the_registered_definition() => _definitionStorage.State.EventTypes.ShouldContainOnly(_eventType);
}
