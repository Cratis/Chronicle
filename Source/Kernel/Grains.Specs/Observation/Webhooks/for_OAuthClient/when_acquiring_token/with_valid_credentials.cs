// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_OAuthClient.when_acquiring_token;

public class with_valid_credentials : given.an_oauth_client
{
    const string ExpectedAccessToken = "test-access-token";
    const int ExpiresInSeconds = 3600;
    static readonly Uri DiscoveryEndpoint = new("https://authority.com/.well-known/openid-configuration");
    static readonly Uri TokenEndpoint = new("https://authority.com/oauth/token");

    OAuthAuthorization _authorization;
    AccessTokenInfo _result;

    void Establish()
    {
        _authorization = new OAuthAuthorization(
            new Authority("https://authority.com"),
            new ClientId("client-id"),
            new ClientSecret("client-secret"));

        SetupSuccessfulDiscoveryResponse(TokenEndpoint.ToString());
        SetupSuccessfulTokenResponse(ExpectedAccessToken, ExpiresInSeconds);
    }

    async Task Because() => _result = await _client.AcquireToken(_authorization);

    [Fact] void should_return_access_token() => _result.AccessToken.ShouldEqual(ExpectedAccessToken);
    [Fact] void should_not_be_expired() => _result.IsExpired.ShouldBeFalse();
    [Fact] void should_have_expiration_in_future() => _result.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    [Fact] void should_request_well_known_configuration() => _messageHandler.Requests[0].RequestUri.ShouldEqual(DiscoveryEndpoint);
    [Fact] void should_request_token_endpoint_from_configuration() => _messageHandler.Requests[1].RequestUri.ShouldEqual(TokenEndpoint);
}
