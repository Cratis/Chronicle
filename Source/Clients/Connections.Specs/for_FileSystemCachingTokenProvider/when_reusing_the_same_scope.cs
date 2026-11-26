// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_reusing_the_same_scope : given.a_caching_provider
{
    const string Scope = "https://server-one:35000|client-one";
    string? _result;

    async Task Establish()
    {
        _provider.Dispose();
        _provider = new(_inner, _cacheFile, null, Scope);
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("cached-token", "fresh-token");
        await _provider.GetAccessToken();
    }

    async Task Because()
    {
        using var other = new FileSystemCachingTokenProvider(_inner, _cacheFile, null, Scope);
        _result = await other.GetAccessToken();
    }

    [Fact] void should_reuse_the_token() => _result.ShouldEqual("cached-token");
    [Fact] void should_acquire_only_once() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_store_only_the_scope_hash() => CacheProperty("ScopeHash").ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Scope))));
    [Fact] void should_not_store_the_scope_in_plain_text() => File.ReadAllText(_cacheFile).Contains(Scope, StringComparison.Ordinal).ShouldBeFalse();
}
