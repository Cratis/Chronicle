// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhook.given;

public class a_webhook_grain_with_replay_on_definition_change : Specification
{
    protected Webhook _grain;
    protected TestKitSilo _silo;
    protected INamespaces _namespacesGrain;
    protected WebhookDefinition _definition;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IWebhookDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, null));
        _silo.AddService(definitionComparer);

        _silo.AddService(Options.Create(new ChronicleOptions
        {
            Observers = new Observers { ReplayOnDefinitionChange = true }
        }));

        _namespacesGrain = Substitute.For<INamespaces>();
        _namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => _namespacesGrain);

        _definition = CreateDefinition("https://example.com/webhook");

        // WebhookDefinition has no parameterless constructor, so the test silo cannot materialize an
        // initial state on its own. Seed the storage with the definition the grain starts out with.
        var storage = Substitute.For<IStorage<WebhookDefinition>>();
        storage.State = CreateDefinition("https://example.com/previous");
        _silo.Options.StorageFactory = _ => storage;

        _grain = await _silo.CreateGrainAsync<Webhook>(new WebhookKey("test-webhook", "test-event-store").ToString());
    }

    static WebhookDefinition CreateDefinition(string url) => new(
        "test-webhook",
        WebhookOwner.Client,
        "event-sequence-id",
        [],
        new WebhookTarget(new WebhookTargetUrl(url), WebhookAuthorization.None, new Dictionary<string, string>()),
        false,
        true);
}
