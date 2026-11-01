// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class while_a_client_serves_the_observer_on_another_sequence : given.a_webhook_registration_with_a_connected_client
{
    Exception _exception;

    void Establish()
    {
        _storage.GetEventStore((EventStoreName)"the-event-store").Observers.Get("the-observer").Returns(new ObserverDefinition
        {
            Identifier = "the-observer",
            EventSequenceId = "the-other-sequence"
        });
        var requestedObserver = Substitute.For<IObserver>();
        requestedObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>(new ObserverKey("the-observer", "the-event-store", "the-namespace", "event-log")).Returns(requestedObserver);
    }

    async Task Because() => _exception = await Catch.Exception(Register);

    [Fact] void should_refuse_the_takeover() => _exception.ShouldBeOfExactType<ObserverServedByAnotherTransport>();
    [Fact] void should_not_append_a_registration() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
}
