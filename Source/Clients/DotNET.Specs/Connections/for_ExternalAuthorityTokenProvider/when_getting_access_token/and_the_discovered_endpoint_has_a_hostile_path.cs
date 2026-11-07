// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_discovered_endpoint_has_a_hostile_path : given.an_external_authority
{
    const string Marker = "hostile-path-marker";

    Exception _insecure;
    Exception _insecureThrottled;
    Exception _rejected;

    void Establish() => _handler.DiscoveryDocument = $"{{\"token_endpoint\":\"http://idp.example.com/{Marker}/token?q={Marker}\"}}";

    async Task Because()
    {
        _insecure = await Catch.Exception(() => _provider.GetAccessToken());
        _insecureThrottled = await Catch.Exception(() => _provider.GetAccessToken());

        _handler.DiscoveryDocument = $"{{\"token_endpoint\":\"https://idp.example.com/{Marker}/token\"}}";
        _handler.TokenStatus = HttpStatusCode.Unauthorized;
        _time.Advance(TimeSpan.FromSeconds(6));
        _rejected = await Catch.Exception(() => _provider.GetAccessToken());
    }

    [Fact] void should_reject_the_insecure_endpoint() => _insecure.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_not_expose_the_path_in_the_rejection() => _insecure.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_path_in_the_throttled_rejection() => _insecureThrottled.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_path_when_the_token_request_is_rejected() => _rejected.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_path() => _log.Entries.Concat(_tokenLog.Entries).Any(_ => _.Contains(Marker)).ShouldBeFalse();
    [Fact] void should_not_log_the_host() => _log.Entries.Concat(_tokenLog.Entries).Any(_ => _.Contains("idp.example.com")).ShouldBeFalse();
    [Fact] void should_log_the_rejected_token_request_with_the_source_only() => _tokenLog.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("discovered token endpoint", StringComparison.Ordinal)).ShouldBeTrue();
}
