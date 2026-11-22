// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_cached_endpoint_is_missing : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("incomplete-token");
        await _provider.GetAccessToken();
        var cache = JsonNode.Parse(await File.ReadAllTextAsync(_cacheFile))!;
        cache["Binding"]!.AsObject().Remove("TokenEndpoint");
        await File.WriteAllTextAsync(_cacheFile, cache.ToJsonString());
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("complete-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_discard_the_incomplete_token() => _result.ShouldEqual("complete-token");
    [Fact] void should_replace_the_incomplete_cache() => CacheProperty("AccessToken").ShouldEqual("complete-token");
}
