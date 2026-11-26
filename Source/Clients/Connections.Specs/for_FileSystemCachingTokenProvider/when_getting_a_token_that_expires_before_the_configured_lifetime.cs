// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_a_token_that_expires_before_the_configured_lifetime : given.a_caching_provider
{
    string _result;

    void Establish() => _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(JwtExpiringIn(TimeSpan.FromSeconds(10)), "fresh-token");

    async Task Because()
    {
        await _provider.GetAccessToken();
        _result = await _provider.GetAccessToken();
    }

    [Fact] void should_use_the_expiry_the_issuer_returned() => _result.ShouldEqual("fresh-token");
}
