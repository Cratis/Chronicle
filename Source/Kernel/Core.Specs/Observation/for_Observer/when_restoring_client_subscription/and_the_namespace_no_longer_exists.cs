// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_namespace_no_longer_exists : given.an_observer_that_lost_its_client
{
    void Establish() => _namespaces.GetAll().Returns([]);

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);

    [Fact] async Task should_not_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_not_know_the_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();
}
