// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider.given;

public class a_persisted_authority_token : a_caching_provider
{
    internal TokenCacheEndpointHandler _handler;
    protected Uri _authority = new("https://idp.example/realm");

    async Task Establish()
    {
        using var first = CreateProvider(new TokenCacheEndpointHandler());
        await first.GetAccessToken();
        _handler = new TokenCacheEndpointHandler();
    }

    protected async Task<string?> ReadPersistedToken()
    {
        _provider = CreateProvider(_handler);
        return await _provider.GetAccessToken();
    }

    FileSystemCachingTokenProvider CreateProvider(HttpMessageHandler handler) => new(
        new ExternalAuthorityTokenProvider(
            _authority,
            "cache-client",
            "test-secret",
            null,
            NullLogger<ExternalAuthorityTokenProvider>.Instance,
            NullLogger<OAuthTokenProvider>.Instance,
            handler,
            TimeProvider.System),
        _cacheFile);
}
