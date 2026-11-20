// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhook.when_the_definition_changes;

public class with_signing_and_target_changed : given.a_webhook_grain_with_replay_on_definition_change
{
    void Establish()
    {
        var comparer = (IWebhookDefinitionComparer)_silo.ServiceProvider.GetService(typeof(IWebhookDefinitionComparer))!;
        comparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, new(false, true, false, false, false, false, false, false) { SigningChanged = true }));
        _definition = _definition with { Signing = WebhookSigningMode.Ed25519 };
    }

    async Task Because() => await _grain.SetDefinition(_definition);
    [Fact] void should_keep_the_existing_replay_behavior() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
