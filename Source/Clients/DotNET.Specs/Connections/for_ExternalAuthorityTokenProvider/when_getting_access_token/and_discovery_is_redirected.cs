// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_is_redirected : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.DiscoveryStatus = HttpStatusCode.TemporaryRedirect;

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_fail_naming_the_authority() => _result.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_not_request_a_token() => _handler.TokenRequests.ShouldBeEmpty();
    [Fact] void should_only_contact_the_authority_once() => _handler.Requests.Count.ShouldEqual(1);
}
