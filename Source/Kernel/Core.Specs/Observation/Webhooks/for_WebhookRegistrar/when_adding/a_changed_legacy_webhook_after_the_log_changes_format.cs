// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class a_changed_legacy_webhook_after_the_log_changes_format : given.a_lagging_legacy_registration
{
    void Establish()
    {
        var legacy = _definition.ToChronicle();
        var envelope = legacy with { DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1 };
        var reads = 0;
        _manager.GetWebhookDefinitions().Returns(_ =>
        {
            TheStoredRegistrationHasFormat(WebhookDeliveryFormat.EnvelopeV1);
            return reads++ == 0 ? [legacy] : [envelope];
        });
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, new(false, true, true, true, false, false, false, false)));
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_format_change_even_when_the_read_model_says_different() => _error.ShouldBeOfExactType<WebhookDeliveryFormatCannotBeChanged>();
    [Fact] void should_not_append_anything() => AppendCount.ShouldEqual(0);
    [Fact] void should_not_append_authorization_or_scope() => AppendManyCount.ShouldEqual(0);
    [Fact] void should_check_the_log_before_comparing() => _webhookDefinitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
}
