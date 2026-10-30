// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorizationJsonConverter;

public class when_reading_bearer : Specification
{
    WebhookAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<WebhookAuthorization>("""{"type":"bearer","token":"token"}""")!;

    [Fact] void should_read_bearer() => _result.AsT1.Token.Value.ShouldEqual("token");
}
