// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_cache_has_expired : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("expired-token");
        await _provider.GetAccessToken();
        var cache = JsonNode.Parse(await File.ReadAllTextAsync(_cacheFile))!;
        cache["Expiry"] = DateTimeOffset.UtcNow.AddMinutes(-1);
        await File.WriteAllTextAsync(_cacheFile, cache.ToJsonString());
        _inner.ClearReceivedCalls();
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("fresh-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_not_reuse_the_expired_token() => _result.ShouldEqual("fresh-token");
    [Fact] void should_acquire_a_token() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_replace_the_expired_cache() => CacheProperty("AccessToken").ShouldEqual("fresh-token");
}
