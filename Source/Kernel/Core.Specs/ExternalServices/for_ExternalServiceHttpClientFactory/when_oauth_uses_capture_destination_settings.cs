// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientFactory;

public class when_oauth_uses_capture_destination_settings : given.a_guarded_capture_client
{
    void Establish()
    {
        _options = new()
        {
            ExternalServices = new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["capture.example", "identity.example", "tokens.example"] },
            Webhooks = new() { Validation = WebhookValidationMode.Enforce }
        };
        _definition = _definition with
        {
            Endpoint = new(ExternalServiceEndpointType.Http, Http: new("https://capture.example", new OAuthAuthorization("https://identity.example", "client", "secret"), new Dictionary<string, string>()))
        };
    }

    async Task Because()
    {
        using var client = await Factory.Create(_definition);
        using var response = await client.GetAsync(string.Empty);
    }

    [Fact] void should_send_discovery_through_the_capture_client() => _transport.Targets.ShouldContain(new Uri("https://identity.example/.well-known/openid-configuration"));
    [Fact] void should_send_the_token_request_through_the_capture_client() => _transport.Targets.ShouldContain(new Uri("https://tokens.example/token"));
    [Fact] void should_send_the_capture_request() => _transport.Targets.ShouldContain(new Uri("https://capture.example"));
}
