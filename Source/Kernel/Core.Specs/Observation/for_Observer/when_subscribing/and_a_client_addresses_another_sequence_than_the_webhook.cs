// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_client_addresses_another_sequence_than_the_webhook : given.an_observer_with_a_transport_on_another_sequence
{
    Exception _exception;

    void Establish() => _owningObserver.GetSubscription().Returns(new ObserverSubscription(
        _observerId, _owningKey, [], typeof(IWebhookObserverSubscriber), SiloAddress.Zero));

    async Task Because() => _exception = await Catch.Exception(() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero, _client));

    [Fact] void should_refuse_the_takeover() => _exception.ShouldBeOfExactType<ObserverServedByAnotherTransport>();
    [Fact] void should_keep_the_original_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_definitionEventSequenceId);
}
