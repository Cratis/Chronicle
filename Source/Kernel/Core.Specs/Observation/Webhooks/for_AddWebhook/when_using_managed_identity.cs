// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhook;

public class when_using_managed_identity : Specification
{
    WebhookTarget _result;

    void Because() => _result = new AddWebhook("orders", "receiver", "https://receiver.example", "event-log", [], AuthorizationType.ManagedIdentity, "", "", "", "", "", "", new Dictionary<string, string>(), true, true)
    {
        ManagedIdentityClientId = "identity",
        ManagedIdentityScope = "api://receiver/.default"
    }.Target();

    [Fact] void should_set_the_client_id() => _result.ManagedIdentity!.ClientId.ShouldEqual("identity");
    [Fact] void should_set_the_scope() => _result.ManagedIdentity!.Scope.ShouldEqual("api://receiver/.default");
    [Fact] void should_leave_the_legacy_authorization_unset() => _result.Authorization.ShouldBeNull();
}
