// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorizationJsonConverter;

public class when_round_tripping_managed_identity : Specification
{
    WebhookAuthorization _result;
    string _json;

    void Because()
    {
        WebhookAuthorization authorization = new ManagedIdentityAuthorization("client", "api://resource/.default");
        _json = JsonSerializer.Serialize(authorization);
        _result = JsonSerializer.Deserialize<WebhookAuthorization>(_json)!;
    }

    [Fact] void should_preserve_the_client_id() => _result.AsT4.ClientId.Value.ShouldEqual("client");
    [Fact] void should_preserve_the_scope() => _result.AsT4.Scope.Value.ShouldEqual("api://resource/.default");
    [Fact] void should_write_the_discriminator() => _json.ShouldContain("managedIdentity");
    [Fact] void should_not_write_a_secret() => _json.ShouldNotContain("secret");
}
