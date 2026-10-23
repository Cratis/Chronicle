// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookAuthorizationJsonConverter;

public class when_reading_an_unknown_type : Specification
{
    WebhookAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<WebhookAuthorization>("{\"type\":\"future\"}")!;

    [Fact] void should_fall_back_to_none() => _result.IsT3.ShouldBeTrue();
}
