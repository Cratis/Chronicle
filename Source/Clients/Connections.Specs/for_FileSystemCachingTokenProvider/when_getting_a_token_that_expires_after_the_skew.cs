// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_a_token_that_expires_after_the_skew : given.a_caching_provider
{
    string _token;
    string _result;

    void Establish()
    {
        _provider.Dispose();
        _provider = new(_inner, _cacheFile, TimeSpan.FromMilliseconds(-1));
        _token = JwtExpiringIn(TimeSpan.FromHours(2));
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(_token, "fresh-token");
    }

    async Task Because()
    {
        await _provider.GetAccessToken();
        _result = await _provider.GetAccessToken();
    }

    [Fact] void should_not_let_the_issuer_extend_the_configured_lifetime() => _result.ShouldEqual("fresh-token");
}
