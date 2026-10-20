// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.ExternalServices;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthHttpMessageHandler;

public class when_an_external_service_client_outlives_its_token : for_OAuthClient.given.a_cached_oauth_client
{
    bool _defaultAuthorization;

    async Task Because()
    {
        using var client = await new ExternalServiceHttpClientFactory(_factory, _client).Create(new("service", "Service", new ExternalServiceEndpoint(ExternalServiceEndpointType.Http, new HttpEndpointConfiguration("https://target.example", _authorization, new Dictionary<string, string>()))));
        _defaultAuthorization = client.DefaultRequestHeaders.Authorization is not null;
        using var first = await client.GetAsync("https://target.example/resource");
        _clock.Advance(TimeSpan.FromHours(1));
        using var second = await client.GetAsync("https://target.example/resource");
    }

    [Fact] void should_not_store_authorization_on_default_headers() => _defaultAuthorization.ShouldBeFalse();
    [Fact] void should_fetch_a_fresh_token() => _endpoint.TokenRequests.ShouldEqual(2);
    [Fact] void should_apply_the_first_token() => _endpoint.SentTokens[0].ShouldEqual("token-1");
    [Fact] void should_apply_the_new_token() => _endpoint.SentTokens[1].ShouldEqual("token-2");
}
