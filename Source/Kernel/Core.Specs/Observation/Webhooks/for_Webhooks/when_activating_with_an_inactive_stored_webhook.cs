// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks;

public class when_activating_with_an_inactive_stored_webhook : Specification
{
    readonly TestKitSilo _silo = new();
    IWebhook _webhook;
    IObserver _observer;
    WebhookDefinition _definition;

    void Establish()
    {
        _webhook = Substitute.For<IWebhook>();
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _webhook);
        _silo.AddProbe(_ => _observer);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([new EventStoreNamespaceName("default")]);
        _silo.AddProbe(_ => namespaces);
        _silo.AddService(Substitute.For<ILocalSiloDetails>());
        _silo.AddService(NullLogger<Webhooks>.Instance);
        _definition = new("inactive", WebhookOwner.Client, EventSequenceId.Log, [], new("http://169.254.169.254/secret", WebhookAuthorization.None, new Dictionary<string, string>()), IsActive: false);
        _silo.StorageManager.GetStorage<WebhooksState>(typeof(Webhooks).FullName).State = new() { Webhooks = [_definition] };
    }

    Task Because() => _silo.CreateGrainAsync<Webhooks>("orders");

    [Fact] void should_preserve_the_inactive_definition() => _webhook.Received().SetDefinition(Arg.Is<WebhookDefinition>(definition => definition == _definition && !definition.IsActive));
    [Fact] void should_not_subscribe_the_observer() => _observer.DidNotReceiveWithAnyArgs().Subscribe<IWebhookObserverSubscriber>(default, default!, default!);
}
