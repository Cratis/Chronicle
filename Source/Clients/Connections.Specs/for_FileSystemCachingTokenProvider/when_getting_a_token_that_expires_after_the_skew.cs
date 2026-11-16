// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_a_token_that_expires_after_the_skew : given.a_caching_provider
{
    string _token;
    string _result;

    void Establish()
    {
        _token = JwtExpiringIn(TimeSpan.FromHours(2));
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(_token, "fresh-token");
    }

    async Task Because()
    {
        await _provider.GetAccessToken();
        _result = await _provider.GetAccessToken();
    }

    [Fact] void should_keep_using_the_cached_token_beyond_the_default_lifetime() => _result.ShouldEqual(_token);
}
