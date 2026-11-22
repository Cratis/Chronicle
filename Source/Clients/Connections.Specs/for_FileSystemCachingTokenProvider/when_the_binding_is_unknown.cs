// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_binding_is_unknown : given.a_caching_provider
{
    ITokenProvider _unknown;
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("previous-token");
        await _provider.GetAccessToken();
        _unknown = Substitute.For<ITokenProvider>();
        _unknown.GetAccessToken(Arg.Any<CancellationToken>()).Returns("unverified-token");
        _provider = new FileSystemCachingTokenProvider(_unknown, _cacheFile);
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_not_reuse_a_persisted_token() => _result.ShouldEqual("unverified-token");
    [Fact] void should_acquire_a_token() => _unknown.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_not_persist_an_unverified_token() => CacheProperty("AccessToken").ShouldEqual("previous-token");
}
