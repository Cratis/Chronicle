// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Security;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.given;

public class a_subscriber_with_signing : Specification
{
    protected WebhookObserverSubscriber _subscriber;
    protected IWebhookMediator _mediator;
    protected AppendedEvent[] _events;
    protected List<WebhookMessageIdentity?> _identities;
    protected WebhookDefinition _definition;
    protected IEventGenerationRelease _release;
    protected List<IEnumerable<AppendedEvent>> _batches;
    protected Storage.Observation.Webhooks.IWebhookDefinitionsStorage _definitions;
    protected virtual WebhookSigningMode Mode => WebhookSigningMode.Ed25519;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _mediator = Substitute.For<IWebhookMediator>();
        _identities = [];
        _batches = [];
        _mediator.Deliver(Arg.Any<EventStoreName>(), Arg.Any<WebhookTarget>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>(), Arg.Any<WebhookMessageIdentity?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _identities.Add(call.Arg<WebhookMessageIdentity?>());
                _batches.Add(call.Arg<IEnumerable<AppendedEvent>>());
                return new WebhookDeliveryResult(EventSequenceNumber.Unavailable);
            });
        silo.AddService(_mediator);
        silo.AddService(Substitute.For<IOAuthClient>());
        silo.AddService(Substitute.For<IEncryption>());
        _definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>())) { Signing = Mode };
        var storage = Substitute.For<IStorage<WebhookDefinition>>();
        storage.State = _definition with { Signing = WebhookSigningMode.None };
        silo.Options.StorageFactory = _ => storage;
        var definitions = Substitute.For<Storage.IStorage>();
        definitions.GetEventStore("store").EventTypes.GetDefinition(Arg.Any<EventTypeId>()).Returns(call => new EventTypeDefinition(call.Arg<EventTypeId>(), EventTypeOwner.None, false, [], []));
        _definitions = definitions.GetEventStore("store").Webhooks;
        _definitions.Get("webhook").Returns(_definition);
        silo.AddService(definitions);
        _release = Substitute.For<IEventGenerationRelease>();
        silo.AddService(_release);
        _subscriber = await silo.CreateGrainAsync<WebhookObserverSubscriber>(new ObserverSubscriberKey("webhook", "store", "namespace", "log", "partition", string.Empty).ToString());
        _events = [AppendedEvent.EmptyWithEventSequenceNumber(42), AppendedEvent.EmptyWithEventSequenceNumber(44)];
    }
}
