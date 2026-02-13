// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;
using Microsoft.Extensions.Logging;
using IWebhooks = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks;

public class when_adding_webhook : Specification
{
    IChronicleConnection _chronicleConnection;
    IEventStore _eventStore;
    IEventTypes _eventTypes;
    Chronicle.Webhooks.Webhooks _webhooks;
    WebhookId _webhookId;
    WebhookTargetUrl _targetUrl;
    IChronicleServicesAccessor _serviceAccessor;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _chronicleConnection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = _chronicleConnection as IChronicleServicesAccessor;
        _serviceAccessor.Services.Returns(Substitute.For<IServices>());
        _serviceAccessor.Services.Webhooks.Returns(Substitute.For<IWebhooks>());
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(new EventStoreName("some-event-store"));
        _eventStore.Connection.Returns(_chronicleConnection);
        _webhookId = "some-webhook-id";
        _targetUrl = "http://localhost/url";
        _webhooks = new Chronicle.Webhooks.Webhooks(_eventTypes, _eventStore, Substitute.For<ILogger<Chronicle.Webhooks.Webhooks>>());
    }

    async Task Because() => await _webhooks.Register(
        _webhookId,
        _targetUrl,
        builder => builder
            .OnEventSequence(EventSequenceId.Log));

    [Fact]
    void should_call_register_on_services_with_webhook() => _serviceAccessor.Services.Webhooks.Received(1)
        .Add(Arg.Is<AddWebhooks>(w =>
            w.EventStore == _eventStore.Name &&
            w.Owner == ObserverOwner.Client &&
            w.Webhooks.Count == 1));
}
