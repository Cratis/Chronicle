// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_transport_throws_a_hostile_exception : given.an_external_authority
{
    const string Marker = "hostile-host-marker.example.net";

    Exception _first;
    Exception _throttled;
    Exception _refreshed;

    void Establish() => _handler.DiscoveryException = new HttpRequestException(
        $"No such host is known ({Marker}:443)",
        new SocketException((int)SocketError.HostNotFound));

    async Task Because()
    {
        _first = await Catch.Exception(() => _provider.GetAccessToken());
        _throttled = await Catch.Exception(() => _provider.GetAccessToken());
        _refreshed = await Catch.Exception(() => _provider.Refresh());
    }

    [Fact] void should_fail_with_a_discovery_failure() => _first.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_fail_the_throttled_retry_with_a_discovery_failure() => _throttled.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_fail_the_bypassing_refresh_with_a_discovery_failure() => _refreshed.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_not_keep_the_exception_on_the_initial_failure() => _first.InnerException.ShouldBeNull();
    [Fact] void should_not_keep_the_exception_on_the_throttled_retry() => _throttled.InnerException.ShouldBeNull();
    [Fact] void should_not_keep_the_exception_on_the_refresh() => _refreshed.InnerException.ShouldBeNull();
    [Fact] void should_not_expose_the_marker_in_the_initial_failure() => _first.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_marker_in_the_throttled_retry() => _throttled.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_marker_in_the_refresh() => _refreshed.ToString().Contains(Marker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_marker() => _log.Entries.Any(_ => _.Contains(Marker) || _.Contains("443")).ShouldBeFalse();
    [Fact] void should_not_log_any_exception() => _log.Entries.All(_ => _.Exception is null).ShouldBeTrue();
    [Fact] void should_log_the_failure_category() => _log.Entries.Any(_ => _.Level == LogLevel.Warning && _.Message.Contains("reason: unreachable", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_discover_again_when_throttled() => _handler.DiscoveryRequests.Count().ShouldEqual(2);
}
