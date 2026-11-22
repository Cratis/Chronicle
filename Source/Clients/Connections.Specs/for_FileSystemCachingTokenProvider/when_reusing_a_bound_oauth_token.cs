// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_reusing_a_bound_oauth_token : given.a_caching_provider
{
    TokenCacheEndpointHandler _handler;
    string? _result;
    string _endpoint;
    string _clientId;

    async Task Establish()
    {
        using var first = CreateProvider(new TokenCacheEndpointHandler());
        await first.GetAccessToken();
        _handler = new TokenCacheEndpointHandler();
        _provider = CreateProvider(_handler);
    }

    async Task Because()
    {
        _result = await _provider.GetAccessToken();
        using var cache = JsonDocument.Parse(await File.ReadAllTextAsync(_cacheFile));
        var binding = cache.RootElement.GetProperty("Binding");
        _endpoint = binding.GetProperty("TokenEndpoint").GetString()!;
        _clientId = binding.GetProperty("ClientId").GetString()!;
    }

    FileSystemCachingTokenProvider CreateProvider(HttpMessageHandler handler) => new(
        new OAuthTokenProvider(
            new Uri("https://idp.example/oauth/token"),
            "cache-client",
            "test-secret",
            null,
            NullLogger<OAuthTokenProvider>.Instance,
            handler,
            TimeProvider.System),
        _cacheFile);

    [Fact] void should_reuse_the_persisted_token() => _result.ShouldEqual("acquired-token");
    [Fact] void should_not_request_another_token() => _handler.TokenRequests.ShouldEqual(0);
    [Fact] void should_persist_the_token_endpoint() => _endpoint.ShouldEqual("https://idp.example/oauth/token");
    [Fact] void should_persist_the_client_id() => _clientId.ShouldEqual("cache-client");
}
