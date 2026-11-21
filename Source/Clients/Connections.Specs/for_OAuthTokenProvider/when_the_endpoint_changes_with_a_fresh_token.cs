// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public class when_the_endpoint_changes_with_a_fresh_token : Specification
{
    OAuthTokenProvider _provider;
    TokenCacheEndpointHandler _handler;
    ChronicleServerAddress _address = new("server-a.example");
    string? _first;
    string? _next;

    async Task Establish()
    {
        _handler = new TokenCacheEndpointHandler { AccessToken = "token-a" };
        _provider = new OAuthTokenProvider(
            () => _address,
            "test-client",
            "test-secret",
            NullLogger<OAuthTokenProvider>.Instance,
            _handler,
            TimeProvider.System);
        _first = await _provider.GetAccessToken();
        _address = new("server-b.example");
        _handler.AccessToken = "token-b";
    }

    async Task Because() => _next = await _provider.GetAccessToken();

    void Destroy() => _provider.Dispose();

    [Fact] void should_initially_acquire_for_the_first_endpoint() => _first.ShouldEqual("token-a");
    [Fact] void should_not_reuse_the_previous_endpoints_fresh_token() => _next.ShouldEqual("token-b");
    [Fact] void should_fetch_again_without_waiting_for_expiry() => _handler.TokenRequests.ShouldEqual(2);
}
