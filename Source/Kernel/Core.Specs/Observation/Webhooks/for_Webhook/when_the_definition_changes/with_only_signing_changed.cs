// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhook.when_the_definition_changes;

public class with_only_signing_changed : given.a_webhook_grain_with_replay_on_definition_change
{
    INotifyWebhookDefinitionsChanged _subscriber;

    async Task Establish()
    {
        var comparer = _silo.ServiceProvider.GetService(typeof(IWebhookDefinitionComparer)) as IWebhookDefinitionComparer;
        comparer!.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, new(false, false, false, false, false, false, false, false) { SigningChanged = true }));
        _subscriber = Substitute.For<INotifyWebhookDefinitionsChanged>();
        await _grain.SubscribeDefinitionsChanged(_subscriber);
        _definition = _definition with { Signing = WebhookSigningMode.Ed25519 };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_persist_and_notify_without_replay_or_recommendations(bool replay)
    {
        _options.Value.Returns(new ChronicleOptions { Observers = new Observers { ReplayOnDefinitionChange = replay } });
        await _grain.SetDefinition(_definition);
        (await _grain.GetDefinition()).Signing.ShouldEqual(WebhookSigningMode.Ed25519);
        await _subscriber.Received(1).OnWebhookDefinitionsChanged();
        await _namespacesGrain.DidNotReceive().GetAll();
        _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    }
}
