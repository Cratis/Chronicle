// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionConverter;

public class when_managed_identity_overrides_legacy_authorization : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _contract;

    void Because()
    {
        var builder = new WebhookDefinitionBuilder(Substitute.For<IEventTypes>());
        builder.WithBearerToken("ignored");
        var definition = builder.Build("webhook", "https://receiver.example");
        _contract = (definition with
        {
            Target = definition.Target with { ManagedIdentity = new ManagedIdentityAuthorization("api://receiver/.default") }
        }).ToContract();
    }

    [Fact] void should_send_managed_identity() => _contract.Target.ManagedIdentity.ShouldNotBeNull();
    [Fact] void should_leave_the_legacy_authorization_unset() => _contract.Target.Authorization.ShouldBeNull();
}
