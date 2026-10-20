// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder.when_building;

public class with_oauth_scope : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _contract;

    void Because()
    {
        var builder = new WebhookDefinitionBuilder(Substitute.For<IEventTypes>());
        builder.WithOAuth("https://authority.example", "client", "secret", "resource/.default");
        _contract = builder.Build("webhook", "https://target.example").ToContract();
    }

    [Fact] void should_send_the_scope_to_the_kernel() => _contract.Target.Authorization!.Value2!.Scope.ShouldEqual("resource/.default");
}
