// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// A stored definition that differs from the registration is reconciled by the full subscribe.
/// </summary>
public class and_another_client_instance_subscribes_while_the_stored_definition_differs : given.an_observer_served_by_one_client_instance
{
    void Establish() => _definitionStorage.State = _definitionStorage.State with
    {
        EventTypes = [new EventType("0c9e8d7f-6a5b-4c3d-8e2f-1a0b9c8d7e6f", EventTypeGeneration.First)]
    };

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_write_the_registered_definition() => _definitionStorage.State.EventTypes.ShouldContainOnly(_eventType);
}
