// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_acquiring_token;

public class with_event_store_only_hosts : given.an_oauth_client
{
    AccessTokenInfo _token;
    WebhookTargetRejected _globalRejection;
    WebhookTargetRejected _otherStoreRejection;

    void Establish()
    {
        SetupSuccessfulDiscoveryResponse("https://tokens.example/token");
        SetupSuccessfulTokenResponse("token", 3600);
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("93.184.216.34")]);
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = new ChronicleOptions
        {
            Webhooks = new()
            {
                Validation = WebhookValidationMode.Enforce,
                EventStoreAllowedHosts = new Dictionary<string, string[]>
                {
                    ["orders"] = ["authority.example", "tokens.example"],
                    ["other"] = ["authority.example"]
                }
            }
        };
        var policy = new WebhookTargetPolicy(Options.Create(options), environment, resolver, NullLogger<WebhookTargetPolicy>.Instance);
        _client = new OAuthClient(_httpClientFactory, policy);
    }

    async Task Because()
    {
        var authorization = new OAuthAuthorization("https://authority.example", "client", "secret");
        _token = await _client.AcquireToken(authorization, "orders");
        try
        {
            await _client.AcquireToken(authorization);
        }
        catch (WebhookTargetRejected error)
        {
            _globalRejection = error;
        }
        try
        {
            await _client.AcquireToken(authorization, "other");
        }
        catch (WebhookTargetRejected error)
        {
            _otherStoreRejection = error;
        }
    }

    [Fact] void should_acquire_a_token_for_the_allowed_store() => _token.AccessToken.ShouldEqual("token");
    [Fact] void should_reject_the_authority_globally_even_when_cached() => _globalRejection.ShouldNotBeNull();
    [Fact] void should_reject_the_cached_token_endpoint_for_another_store() => _otherStoreRejection.ShouldNotBeNull();
    [Fact] void should_only_send_discovery_and_token_requests_for_the_allowed_store() => _messageHandler.Requests.Count.ShouldEqual(2);
    [Fact] void should_tag_discovery_with_the_store() => _messageHandler.Requests[0].Options.GetEventStore().ShouldEqual((Concepts.EventStoreName)"orders");
    [Fact] void should_tag_token_acquisition_with_the_store() => _messageHandler.Requests[1].Options.GetEventStore().ShouldEqual((Concepts.EventStoreName)"orders");
}
