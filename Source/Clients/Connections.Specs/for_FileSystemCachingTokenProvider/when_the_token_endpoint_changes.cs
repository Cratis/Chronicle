// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_token_endpoint_changes : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("old-endpoint-token");
        await _provider.GetAccessToken();
        _inner.ClearReceivedCalls();
        var binding = new TokenCacheBinding("https://other.example/connect/token", "client-one");
        ((ITokenCacheBindingProvider)_inner).GetCurrentCacheBinding(Arg.Any<CancellationToken>()).Returns(binding);
        ((ITokenCacheBindingProvider)_inner).AcquiredTokenBinding.Returns(binding);
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("new-endpoint-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_not_reuse_the_other_endpoints_token() => _result.ShouldEqual("new-endpoint-token");
    [Fact] void should_acquire_a_token() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_replace_the_cached_token() => CacheProperty("AccessToken").ShouldEqual("new-endpoint-token");
}
