// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_discovery_document_has_no_token_endpoint : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.DiscoveryDocument = "{\"issuer\":\"https://idp.example.com\"}";

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_throw_discovery_failed() => _result.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_name_the_insecure_endpoint_as_the_cause() => _result.InnerException.ShouldBeOfExactType<InsecureTokenEndpoint>();
    [Fact] void should_not_post_credentials() => _handler.TokenRequests.ShouldBeEmpty();
}
