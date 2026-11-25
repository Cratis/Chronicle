// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_cache_has_no_binding : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("legacy-token");
        await _provider.GetAccessToken();
        var cache = JsonNode.Parse(await File.ReadAllTextAsync(_cacheFile))!.AsObject();
        cache.Remove("Binding");
        await File.WriteAllTextAsync(_cacheFile, cache.ToJsonString());
        _inner.ClearReceivedCalls();
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("bound-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_discard_the_legacy_token() => _result.ShouldEqual("bound-token");
    [Fact] void should_acquire_a_token() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_replace_the_legacy_cache() => CacheProperty("AccessToken").ShouldEqual("bound-token");
}
