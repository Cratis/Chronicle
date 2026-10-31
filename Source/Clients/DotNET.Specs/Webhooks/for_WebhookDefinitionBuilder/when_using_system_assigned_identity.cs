// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder;

public class when_using_system_assigned_identity : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _contract;

    void Because()
    {
        var builder = new WebhookDefinitionBuilder(Substitute.For<IEventTypes>());
        builder.WithManagedIdentity("api://receiver/.default");
        _contract = builder.Build("webhook", "https://receiver.example").ToContract();
    }

    [Fact] void should_send_an_empty_client_id() => _contract.Target.ManagedIdentity!.ClientId.ShouldEqual(string.Empty);
    [Fact] void should_send_the_scope() => _contract.Target.ManagedIdentity!.Scope.ShouldEqual("api://receiver/.default");
}
