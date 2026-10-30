// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorizationJsonConverter;

public class when_reading_oauth : Specification
{
    WebhookAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<WebhookAuthorization>("""{"type":"oauth","authority":"https://authority.example","clientId":"client","clientSecret":"secret"}""")!;

    [Fact] void should_read_oauth() => _result.IsT2.ShouldBeTrue();
    [Fact] void should_default_legacy_scope() => _result.AsT2.Scope.ShouldEqual(string.Empty);
}
