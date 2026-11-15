// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_discovered_endpoint_is_not_https : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.DiscoveryDocument = "{\"token_endpoint\":\"http://idp.example.com/token\"}";

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_throw_discovery_failed() => _result.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_report_the_invalid_response_category() => _result.Message.Contains("reason: invalid response", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_keep_the_cause() => _result.InnerException.ShouldBeNull();
    [Fact] void should_not_name_the_endpoint() => _result.Message.Contains("idp.example.com", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_post_credentials() => _handler.TokenRequests.ShouldBeEmpty();
}
