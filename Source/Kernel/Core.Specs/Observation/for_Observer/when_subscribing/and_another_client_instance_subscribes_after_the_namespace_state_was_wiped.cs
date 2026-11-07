// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Stored state that no longer carries this activation's alert lifecycle belongs to another world. Handing over in
/// memory would keep a cursor from the old world and skip history, so the full subscribe reloads it.
/// </summary>
public class and_another_client_instance_subscribes_after_the_namespace_state_was_wiped : given.an_observer_served_by_one_client_instance
{
    void Establish() => _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(_ => ObserverState.Empty);

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
