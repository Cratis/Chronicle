// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// Stored state that no longer carries this activation's alert lifecycle belongs to another world, so the full
/// reconcile reloads it.
/// </summary>
public class and_another_instance_of_the_client_is_observing_after_the_namespace_state_was_wiped : given.an_observer_restoring_another_client_instance
{
    void Establish() => _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(_ => ObserverState.Empty);

    Task Because() => RestoreSecondClient();

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
