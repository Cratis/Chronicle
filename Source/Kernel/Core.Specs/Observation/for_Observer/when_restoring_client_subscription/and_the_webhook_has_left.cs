// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_webhook_has_left : given.a_webhook_observer_with_a_waiting_client
{
    Task Establish() => _observer.Unsubscribe();

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, ["Event1", "Event2"], SiloAddress.Zero, _client);

    [Fact] async Task should_subscribe_the_client() => (await _observer.GetSubscription()).Targets.Single().ConnectedClient.ShouldEqual(_client);
}
