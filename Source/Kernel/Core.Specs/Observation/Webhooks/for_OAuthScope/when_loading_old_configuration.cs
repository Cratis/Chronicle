// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthScope;

public class when_loading_old_configuration : Specification
{
    WebhookAuthorization _webhook;
    ExternalServiceAuthorization _service;

    void Because()
    {
        const string configuration = """{"type":"oauth","authority":"https://authority.example","clientId":"client","clientSecret":"secret"}""";
        _webhook = JsonSerializer.Deserialize<WebhookAuthorization>(configuration)!;
        _service = JsonSerializer.Deserialize<ExternalServiceAuthorization>(configuration)!;
    }

    [Fact] void should_default_webhook_scope_to_empty() => _webhook.AsT2.Scope.ShouldEqual(string.Empty);
    [Fact] void should_default_service_scope_to_empty() => _service.AsT2.Scope.ShouldEqual(string.Empty);
}
