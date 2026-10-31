// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_oauth_and_an_empty_batch : Specification
{
    IOAuthClient _tokens;
    IWebhookMediator _mediator;
    WebhookObserverSubscriber _subscriber;
    OAuthAuthorization _decrypted;
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _decrypted = new("https://authority.example", "client", "secret") { Scope = "api://target/.default" };
        var target = new WebhookTarget("https://receiver.example", _decrypted with { ClientSecret = "encrypted" }, new Dictionary<string, string>());
        var definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [], target);
        var state = Substitute.For<IStorage<WebhookDefinition>>();
        state.State = definition;
        silo.Options.StorageFactory = _ => state;
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore("orders").Webhooks.Get("webhook").Returns(definition);
        var encryption = Substitute.For<IEncryption>();
        encryption.Decrypt("encrypted").Returns("secret");
        _tokens = Substitute.For<IOAuthClient>();
        _mediator = Substitute.For<IWebhookMediator>();
        _mediator.Deliver((EventStoreName)"orders", Arg.Any<WebhookTarget>(), Arg.Any<Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(new WebhookDeliveryResult(Concepts.Events.EventSequenceNumber.Unavailable));
        silo.AddService(storage);
        silo.AddService(encryption);
        silo.AddService(_tokens);
        silo.AddService(_mediator);
        silo.AddService(Substitute.For<IEventGenerationRelease>());
        _subscriber = await silo.CreateGrainAsync<WebhookObserverSubscriber>(new ObserverSubscriberKey("webhook", "orders", "namespace", "log", "partition", string.Empty).ToString());
    }

    async Task Because()
    {
        await _subscriber.OnWebhookDefinitionsChanged();
        _result = await _subscriber.OnNext("partition", [], new(null));
    }

    [Fact] async Task should_authorize_activation_for_the_store() => await _tokens.Received(1).AcquireToken(_decrypted, (EventStoreName)"orders", Arg.Any<CancellationToken>());
    [Fact] async Task should_authorize_definition_changes_for_the_store() => await _tokens.Received(1).AcquireToken(_decrypted, (EventStoreName)"orders");
    [Fact] async Task should_deliver_with_the_store_and_decrypted_scoped_configuration() => await _mediator.Received(1).Deliver((EventStoreName)"orders", Arg.Is<WebhookTarget>(_ => _.Authorization.AsT2 == _decrypted), Arg.Any<Key>(), Arg.Is<IEnumerable<Concepts.Events.AppendedEvent>>(_ => !_.Any()), null, null, Arg.Any<CancellationToken>());
    [Fact] void should_accept_the_empty_batch() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
