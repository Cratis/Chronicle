// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class while_a_connected_client_serves_the_observer : given.a_webhook_registrar
{
    IEventSequence _eventSequence;
    Exception _exception;

    void Establish()
    {
        _eventSequence = AnEventSequenceThatAppendsSuccessfully();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(["the-namespace"]);
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(namespaces);
        var observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(new ObserverSubscription("test-webhook", new ObserverKey("test-webhook", "test-event-store", "the-namespace", EventSequenceId.Log), [], typeof(NullObserverSubscriber), SiloAddress.Zero)
        {
            Targets = [new ObserverSubscriberTarget(SiloAddress.Zero, new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" })]
        });
        var availableObserver = Substitute.For<IObserver>();
        availableObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(availableObserver);
        _grainFactory.GetGrain<IObserver>(new ObserverKey("test-webhook", "test-event-store", "the-namespace", EventSequenceId.Log)).Returns(observer);
        var webhooks = Substitute.For<IWebhooks>();
        webhooks.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(webhooks);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));
    }

    async Task Because() => _exception = await Catch.Exception(() => _registrar.Add("test-event-store", [
        new ContractWebhookDefinition { Identifier = "available-webhook", EventSequenceId = EventSequenceId.Log, IsActive = true },
        new ContractWebhookDefinition { Identifier = "test-webhook", EventSequenceId = EventSequenceId.Log, IsActive = true }
    ]));

    [Fact] void should_refuse_the_registration() => _exception.ShouldBeOfExactType<ObserverServedByAnotherTransport>();
    [Fact] void should_not_append_an_event() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_compare_definitions() => _webhookDefinitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
}
