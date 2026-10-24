// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_acquiring_token;

public class and_an_event_store_is_supplied : given.an_oauth_client
{
    void Establish()
    {
        SetupSuccessfulDiscoveryResponse("https://authority.example/token");
        SetupSuccessfulTokenResponse("token", 3600);
    }

    async Task Because() => await _client.AcquireToken(new OAuthAuthorization("https://authority.example", "client", "secret"), "orders");

    [Fact] void should_tag_discovery() => _messageHandler.Requests[0].Options.GetEventStore().ShouldEqual((EventStoreName)"orders");
    [Fact] void should_tag_the_token_request() => _messageHandler.Requests[1].Options.GetEventStore().ShouldEqual((EventStoreName)"orders");
}
