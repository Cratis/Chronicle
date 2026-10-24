// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder;

public class when_replacing_managed_identity : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition[] _contracts;

    void Because() => _contracts = new Action<IWebhookDefinitionBuilder>[]
    {
        _ => _.WithBasicAuth("user", "password"),
        _ => _.WithBearerToken("token"),
        _ => _.WithOAuth("https://authority.example", "client", "secret")
    }.Select(configure =>
    {
        var builder = new WebhookDefinitionBuilder(Substitute.For<IEventTypes>());
        builder.WithManagedIdentity("api://receiver/.default");
        configure(builder);
        return builder.Build("webhook", "https://receiver.example").ToContract();
    }).ToArray();

    [Fact] void should_clear_managed_identity() => _contracts.ShouldEachConformTo(_ => _.Target.ManagedIdentity is null);
    [Fact] void should_use_the_last_authorization_selected() => _contracts.ShouldEachConformTo(_ => _.Target.Authorization is not null);
}
