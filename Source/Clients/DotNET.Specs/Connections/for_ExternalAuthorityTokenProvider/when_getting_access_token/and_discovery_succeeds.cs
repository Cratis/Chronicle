// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_succeeds : given.an_external_authority
{
    string _token;

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_discover_once() => _handler.DiscoveryRequests.Single().Uri.AbsoluteUri.ShouldEqual(DiscoveryUrl);
    [Fact] void should_use_get_for_discovery() => _handler.DiscoveryRequests.Single().Method.ShouldEqual(HttpMethod.Get);
    [Fact] void should_post_once_to_the_discovered_endpoint() => _handler.TokenRequests.Single().Uri.AbsoluteUri.ShouldEqual(TokenUrl);
    [Fact] void should_send_the_scope() => _handler.TokenRequests.Single().Form["scope"].ShouldEqual("api://chronicle/.default");
    [Fact] void should_send_the_client_id() => _handler.TokenRequests.Single().Form["client_id"].ShouldEqual("test-client");
    [Fact] void should_not_log_the_token_source_at_information_level() => _log.Entries.Any(_ => _.Level >= Microsoft.Extensions.Logging.LogLevel.Information).ShouldBeFalse();
    [Fact] void should_return_the_token() => _token.ShouldEqual("the-token");
}
