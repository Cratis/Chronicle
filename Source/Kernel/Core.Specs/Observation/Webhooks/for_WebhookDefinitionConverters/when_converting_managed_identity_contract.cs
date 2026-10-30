// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Security;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters;

public class when_converting_managed_identity_contract : Specification
{
    Concepts.Observation.Webhooks.WebhookTarget _concept;
    WebhookTarget _contract;

    void Because()
    {
        var target = new WebhookTarget
        {
            Url = "https://receiver.example",
            Headers = new Dictionary<string, string> { ["custom"] = "value" },
            Authorization = new(new BearerTokenAuthorization { Token = "ignored" }),
            ManagedIdentity = new ManagedIdentityAuthorization { ClientId = "identity", Scope = "api://receiver/.default" }
        };
        _concept = target.ToChronicle();
        using var payload = new MemoryStream();
        Serializer.Serialize(payload, _concept.ToContract());
        payload.Position = 0;
        _contract = Serializer.Deserialize<WebhookTarget>(payload);
    }

    [Fact] void should_prefer_managed_identity_over_the_legacy_authorization() => _concept.Authorization.IsT4.ShouldBeTrue();
    [Fact] void should_convert_the_client_id() => _concept.Authorization.AsT4.ClientId.Value.ShouldEqual("identity");
    [Fact] void should_convert_the_scope() => _concept.Authorization.AsT4.Scope.Value.ShouldEqual("api://receiver/.default");
    [Fact] void should_send_the_client_id_back() => _contract.ManagedIdentity!.ClientId.ShouldEqual("identity");
    [Fact] void should_send_the_scope_back() => _contract.ManagedIdentity!.Scope.ShouldEqual("api://receiver/.default");
    [Fact] void should_leave_the_legacy_authorization_unset() => _contract.Authorization.ShouldBeNull();
    [Fact] void should_preserve_the_url() => _contract.Url.ShouldEqual("https://receiver.example");
    [Fact] void should_preserve_headers() => _contract.Headers["custom"].ShouldEqual("value");
    [Fact] void should_report_the_authorization_kind() => _concept.Authorization.ToAuthorizationType().ShouldEqual(AuthorizationType.ManagedIdentity);
}
