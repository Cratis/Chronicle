// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder.when_building;

public class with_ed25519_signing : Specification
{
    WebhookDefinitionBuilder _builder;
    WebhookDefinition _result;
    IWebhookDefinitionBuilder _continuation;

    void Establish() => _builder = new(Substitute.For<IEventTypes>());

    void Because()
    {
        _continuation = _builder.WithSigning(WebhookSigningMode.Ed25519);
        _result = _builder.Build("webhook", "https://example.com/webhook");
    }

    [Fact] void should_opt_in() => _result.Signing.ShouldEqual(WebhookSigningMode.Ed25519);
    [Fact] void should_send_ed25519_to_the_kernel() => _result.ToContract().Signing.ShouldEqual(Contracts.Observation.Webhooks.WebhookSigningMode.Ed25519);
    [Fact] void should_continue_with_the_same_builder() => _continuation.ShouldEqual(_builder);
}
