// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Security;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_stale_generation : Specification
{
    WebhookObserverSubscriber _grain;
    IWebhookMediator _mediator;
    WebhookDefinition _definition;
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _mediator = Substitute.For<IWebhookMediator>();
        _mediator.OnNext("store", Arg.Any<WebhookTarget>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>()).Returns(Cratis.Monads.Catch.Success());
        silo.AddService(_mediator);
        silo.AddService(Substitute.For<IOAuthClient>());
        silo.AddService(Substitute.For<IEncryption>());
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 1)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        var generationRelease = Substitute.For<IEventGenerationRelease>();
        generationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => [((IEnumerable<AppendedEvent>)call[3]).Single() with { Context = _event.Context with { EventType = new("customer", 1) } }]);
        silo.AddService(generationRelease);
        var definitionStorage = Substitute.For<Cratis.Chronicle.Storage.IStorage>();
        definitionStorage.GetEventStore("store").Webhooks.Get("webhook").Returns(_definition);
        silo.AddService(definitionStorage);
        var storage = Substitute.For<IStorage<WebhookDefinition>>();
        storage.State = _definition;
        silo.Options.StorageFactory = _ => storage;
        _grain = await silo.CreateGrainAsync<WebhookObserverSubscriber>(new ObserverSubscriberKey("webhook", "store", "namespace", "log", "customer-1", string.Empty).ToString());
        _event = new(EventContext.EmptyWithEventSourceId("customer-1") with { EventType = new("customer", 2), SequenceNumber = 42, AppendedGeneration = 2 }, new ExpandoObject());
    }

    async Task Because() => _result = await _grain.OnNext(new Key("customer-1", ArrayIndexers.NoIndexers), [_event], new(null));

    [Fact] async Task should_deliver_the_current_pin() => await _mediator.Received(1).OnNext("store", _definition.Target, Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Single().Context.EventType == new EventType("customer", 1)), Arg.Any<string?>(), Arg.Any<TimeSpan?>());
    [Fact] void should_acknowledge_the_delivered_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
}
