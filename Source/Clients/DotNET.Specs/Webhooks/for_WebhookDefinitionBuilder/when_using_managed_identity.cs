// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder;

public class when_using_managed_identity : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _contract;
    WebhookDefinition _definition;

    void Because()
    {
        var builder = new WebhookDefinitionBuilder(Substitute.For<IEventTypes>());
        ((IWebhookDefinitionBuilder)builder).WithManagedIdentity("api://receiver/.default", "identity");
        _definition = builder.Build("webhook", "https://receiver.example");
        _contract = _definition.ToContract();
    }

    [Fact] void should_expose_the_selected_identity() => _definition.Target.ManagedIdentity!.ClientId.ShouldEqual("identity");
    [Fact] void should_send_the_client_id() => _contract.Target.ManagedIdentity!.ClientId.ShouldEqual("identity");
    [Fact] void should_send_the_scope() => _contract.Target.ManagedIdentity!.Scope.ShouldEqual("api://receiver/.default");
    [Fact] void should_leave_the_legacy_authorization_unset() => _contract.Target.Authorization.ShouldBeNull();
}
