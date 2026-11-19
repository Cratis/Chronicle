// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder.when_building;

public class without_signing : Specification
{
    WebhookDefinitionBuilder _builder;
    WebhookDefinition _result;

    void Establish() => _builder = new(Substitute.For<IEventTypes>());

    void Because() => _result = _builder.Build("webhook", "https://example.com/webhook");

    [Fact] void should_default_to_unsigned() => _result.Signing.ShouldEqual(WebhookSigningMode.None);
    [Fact] void should_explicitly_disable_signing_in_the_contract() => _result.ToContract().Signing.ShouldEqual(Contracts.Observation.Webhooks.WebhookSigningMode.Disabled);
}
