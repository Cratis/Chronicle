// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class a_webhook_registration_with_a_connected_client : a_webhook_registrar
{
    protected ContractWebhookDefinition _definition;
    protected IObserver _clientObserver;
    protected IEventSequence _eventSequence;

    void Establish()
    {
        _definition = new() { Identifier = "the-observer", EventSequenceId = "event-log" };
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(["the-namespace"]);
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(namespaces);
        _clientObserver = Substitute.For<IObserver>();
        _clientObserver.GetSubscription().Returns(new ObserverSubscription(
            "the-observer",
            new ObserverKey("the-observer", "the-event-store", "the-namespace", EventSequenceId.Log),
            [],
            typeof(NullObserverSubscriber),
            SiloAddress.Zero,
            new ConnectedClient { ConnectionId = "the-connection" })
        {
            Targets = [new ObserverSubscriberTarget(SiloAddress.Zero, new ConnectedClient { ConnectionId = "the-connection" })]
        });
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(_clientObserver);
        _eventSequence = AnEventSequenceThatAppendsSuccessfully();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        var webhooks = Substitute.For<IWebhooks>();
        webhooks.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(webhooks);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));
    }

    protected Task Register() => _registrar.Add("the-event-store", [_definition]);

    protected void ShouldAppend() => _eventSequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).ShouldBeTrue();
}
