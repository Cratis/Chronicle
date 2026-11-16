// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_a_scoped_provider_finds_an_unscoped_entry : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("unscoped-token", "fresh-token");
        await _provider.GetAccessToken();
        var entry = JsonNode.Parse(await File.ReadAllTextAsync(_cacheFile))!.AsObject();
        entry.Remove("ScopeHash");
        await File.WriteAllTextAsync(_cacheFile, entry.ToJsonString());
    }

    async Task Because()
    {
        using var scoped = new FileSystemCachingTokenProvider(_inner, _cacheFile, null, "server-one|client-one");
        _result = await scoped.GetAccessToken();
    }

    [Fact] void should_reacquire() => _result.ShouldEqual("fresh-token");
    [Fact] void should_bind_the_replacement() => CacheProperty("ScopeHash").Length.ShouldEqual(64);
}
