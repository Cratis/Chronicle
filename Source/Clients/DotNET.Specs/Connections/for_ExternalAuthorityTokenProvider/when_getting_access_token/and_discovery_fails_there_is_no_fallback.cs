// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_fails_there_is_no_fallback : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.FailDiscovery = true;

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_fail_naming_the_authority() => _result.Message.Contains("Authentication:Authority", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_request_a_token_from_anywhere() => _handler.TokenRequests.ShouldBeEmpty();
    [Fact] void should_not_contact_the_server_token_endpoint() => _handler.Requests.Exists(_ => _.Uri.AbsolutePath.EndsWith("/connect/token", StringComparison.Ordinal)).ShouldBeFalse();
}
