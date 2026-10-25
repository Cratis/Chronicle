// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_adding;

public class while_a_connected_client_serves_the_observer : given.a_webhooks_manager_grain
{
    WebhookDefinition _definition;

    void Establish()
    {
        _definition = new("the-webhook", WebhookOwner.Kernel, EventSequenceId.Log, [], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()));
        _namespacesGrain.GetAll().Returns(["the-namespace"]);
        var observer = Substitute.For<IObserver>();
        observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey(_definition.Identifier, "test-event-store", "the-namespace", EventSequenceId.Log)));
        _silo.AddProbe(_ => observer);
    }

    Task Because() => _grain.Add(_definition);

    [Fact] void should_keep_the_subscription_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "webhook-subscribe:the-webhook", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)), Times.Once);
    [Fact] void should_keep_the_definition_for_retry() => _stateStorage.State.Webhooks.Single().ShouldEqual(_definition);
}
