// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_client_id_changes : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("old-client-token");
        await _provider.GetAccessToken();
        _inner.ClearReceivedCalls();
        var binding = new TokenCacheBinding("https://server.example/connect/token", "client-two");
        ((ITokenCacheBindingProvider)_inner).GetCurrentCacheBinding(Arg.Any<CancellationToken>()).Returns(binding);
        ((ITokenCacheBindingProvider)_inner).AcquiredTokenBinding.Returns(binding);
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("new-client-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_not_reuse_the_other_clients_token() => _result.ShouldEqual("new-client-token");
    [Fact] void should_acquire_a_token() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_replace_the_cached_token() => CacheProperty("AccessToken").ShouldEqual("new-client-token");
}
