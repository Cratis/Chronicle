// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_fails : given.an_external_authority
{
    Exception _firstResult;
    string _secondToken;

    void Establish() => _handler.FailDiscovery = true;

    async Task Because()
    {
        _firstResult = await Catch.Exception(() => _provider.GetAccessToken());
        _handler.FailDiscovery = false;
        _time.Advance(TimeSpan.FromSeconds(6));
        _secondToken = (await _provider.GetAccessToken())!;
    }

    [Fact] void should_fail_the_first_request() => _firstResult.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_name_the_authority_in_the_error() => _firstResult.Message.Contains("Authentication:Authority", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_cause() => _firstResult.InnerException.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_not_cache_the_failure() => _secondToken.ShouldEqual("the-token");
    [Fact] void should_discover_again() => _handler.DiscoveryRequests.Count().ShouldEqual(2);
}
