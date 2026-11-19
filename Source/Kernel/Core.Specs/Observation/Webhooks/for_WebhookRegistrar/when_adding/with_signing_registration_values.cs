// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using ContractMode = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningMode;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_signing_registration_values : given.a_pinned_registration
{
    WebhookDefinition? _compared;

    void Establish()
    {
        _manager.GetWebhookDefinitions().Returns([_definition.ToChronicle() with { Signing = WebhookSigningMode.Ed25519 }]);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(call =>
            {
                _compared = call.ArgAt<WebhookDefinition>(2);
                return _compared.Signing == WebhookSigningMode.Ed25519
                    ? new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Same, null)
                    : new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, new(false, false, false, false, false, false, false, false) { SigningChanged = true });
            });
    }

    [Theory]
    [InlineData(ContractMode.Unspecified, WebhookSigningMode.Ed25519)]
    [InlineData(ContractMode.Disabled, WebhookSigningMode.None)]
    [InlineData(ContractMode.Ed25519, WebhookSigningMode.Ed25519)]
    async Task should_resolve_the_policy_before_comparing(ContractMode requested, WebhookSigningMode expected)
    {
        _definition.Signing = requested;
        await _registrar.Add("store", [_definition]);
        _compared!.Signing.ShouldEqual(expected);
        if (requested == ContractMode.Disabled)
        {
            _appended.OfType<SigningSetForWebhook>().Single().Signing.ShouldEqual(expected);
        }
        else
        {
            _appended.ShouldBeEmpty();
        }
        _appended.OfType<WebhookAdded>().ShouldBeEmpty();
        _appended.OfType<EventTypesSetForWebhook>().ShouldBeEmpty();
    }
}
