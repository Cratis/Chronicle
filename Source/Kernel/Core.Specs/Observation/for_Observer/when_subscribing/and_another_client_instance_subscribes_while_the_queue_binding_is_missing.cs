// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// An observing observer that lost its appended-events queue binding needs the recovery the full subscribe runs.
/// </summary>
public class and_another_client_instance_subscribes_while_the_queue_binding_is_missing : given.an_observer_served_by_one_client_instance
{
    void Establish() => _appendedEventsQueues.IsSubscribed(Arg.Any<ObserverKey>()).Returns(false);

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
