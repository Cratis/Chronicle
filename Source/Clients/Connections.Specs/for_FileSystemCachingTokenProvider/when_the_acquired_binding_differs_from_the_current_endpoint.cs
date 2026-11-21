// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_acquired_binding_differs_from_the_current_endpoint : given.a_caching_provider
{
    string _persistedEndpoint;

    void Establish()
    {
        var binding = new TokenCacheBinding("https://other.example/connect/token", "client-one");
        ((ITokenCacheBindingProvider)_inner).GetCurrentCacheBinding(Arg.Any<CancellationToken>()).Returns(binding);
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("previous-endpoint-token");
    }

    async Task Because()
    {
        await _provider.GetAccessToken();
        using var cache = JsonDocument.Parse(await File.ReadAllTextAsync(_cacheFile));
        _persistedEndpoint = cache.RootElement.GetProperty("Binding").GetProperty("TokenEndpoint").GetString()!;
        await _provider.GetAccessToken();
    }

    [Fact] void should_persist_the_acquired_binding_not_the_new_endpoint() => _persistedEndpoint.ShouldEqual("https://server.example/connect/token");
    [Fact] void should_not_reuse_the_token_for_the_new_endpoint() => _inner.Received(2).GetAccessToken(Arg.Any<CancellationToken>());
}
