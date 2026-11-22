// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_inner_refreshes_before_a_refresh_is_persisted : given.an_interleaved_token_acquisition
{
    string? _first;
    string? _direct;
    string? _next;

    async Task Because()
    {
        _first = await _provider.Refresh();
        ReadPersistedSnapshot();
        _direct = await _oauth.GetAccessToken();
        _next = await _provider.GetAccessToken();
    }

    [Fact] void should_return_the_original_snapshot() => _first.ShouldEqual("token-a");
    [Fact] void should_allow_the_direct_refresh_to_finish_before_persistence() => _direct.ShouldEqual("token-b");
    [Fact] void should_persist_the_original_token() => _persistedToken.ShouldEqual("token-a");
    [Fact] void should_persist_the_original_binding() => _persistedEndpoint.ShouldEqual(_initialBinding.TokenEndpoint);
    [Fact] void should_not_accept_the_original_token_for_the_new_endpoint() => _next.ShouldEqual("token-b");
}
