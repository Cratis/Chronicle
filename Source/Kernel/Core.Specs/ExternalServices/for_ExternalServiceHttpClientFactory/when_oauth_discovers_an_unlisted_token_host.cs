// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientFactory;

public class when_oauth_discovers_an_unlisted_token_host : given.a_guarded_capture_client
{
    Exception _error;

    void Establish()
    {
        _options = new()
        {
            ExternalServices = new() { Validation = WebhookValidationMode.Enforce, AllowedHosts = ["capture.example", "identity.example"] },
            Webhooks = new() { AllowedHosts = ["tokens.example"] }
        };
        _definition = _definition with
        {
            Endpoint = new(ExternalServiceEndpointType.Http, Http: new("https://capture.example", new OAuthAuthorization("https://identity.example", "client", "secret"), new Dictionary<string, string>()))
        };
    }

    async Task Because()
    {
        using var client = await Factory.Create(_definition);
        _error = await Catch.Exception(async () =>
        {
            using var response = await client.GetAsync(string.Empty);
        });
    }

    [Fact] void should_reject_the_token_host() => _error.ShouldBeOfExactType<WebhookTargetRejected>();
    [Fact] void should_not_send_the_token_request() => _transport.Targets.ShouldNotContain(new Uri("https://tokens.example/token"));
}
