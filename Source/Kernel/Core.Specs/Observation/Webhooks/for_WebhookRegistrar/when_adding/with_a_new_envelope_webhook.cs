// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_new_envelope_webhook : given.a_pinned_registration
{
    void Establish()
    {
        _definition.DeliveryFormat = Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        _definition.Target.Authorization = new(new Contracts.Security.OAuthAuthorization { Authority = "https://example.com", ClientId = "client", ClientSecret = "secret", Scope = "scope" });
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_refuse_the_unsupported_format() => _error.ShouldBeOfExactType<WebhookDeliveryFormatNotSupported>();
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
    [Fact] void should_not_append_authorization_or_scope() => AppendManyCount.ShouldEqual(0);
    [Fact] void should_not_compare() => _webhookDefinitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
}
