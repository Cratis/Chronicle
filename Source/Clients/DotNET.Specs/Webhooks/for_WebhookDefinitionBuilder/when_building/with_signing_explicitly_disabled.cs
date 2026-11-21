// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder.when_building;

public class with_signing_explicitly_disabled : Specification
{
    WebhookDefinitionBuilder _builder;
    WebhookDefinition _result;

    void Establish()
    {
        _builder = new(Substitute.For<IEventTypes>());
        _builder.WithSigning(WebhookSigningMode.Ed25519);
    }

    void Because()
    {
        _builder.WithSigning(WebhookSigningMode.None);
        _result = _builder.Build("webhook", "https://example.com/webhook");
    }

    [Fact] void should_be_unsigned() => _result.Signing.ShouldEqual(WebhookSigningMode.None);
    [Fact] void should_send_disabled_to_the_kernel() => _result.ToContract().Signing.ShouldEqual(Contracts.Observation.Webhooks.WebhookSigningMode.Disabled);
}
