// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorizationJsonConverter;

public class when_reading_managed_identity_without_client : Specification
{
    WebhookAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<WebhookAuthorization>("""{"type":"managedIdentity","scope":"api://resource/.default"}""")!;

    [Fact] void should_use_the_default_identity() => _result.AsT4.ClientId.ShouldEqual(Cratis.Chronicle.Concepts.Security.ManagedIdentityClientId.NotSet);
}
