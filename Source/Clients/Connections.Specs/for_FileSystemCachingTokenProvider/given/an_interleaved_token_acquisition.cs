// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider.given;

public class an_interleaved_token_acquisition : a_caching_provider
{
    internal TokenCacheBinding _initialBinding;
    protected OAuthTokenProvider _oauth;
    protected string _persistedToken;
    protected string _persistedEndpoint;
    ChronicleServerAddress _address = new("server-a.example");

    async Task Establish()
    {
        var handler = new TokenCacheEndpointHandler { AccessToken = "token-a" };
        _oauth = new OAuthTokenProvider(
            () => _address,
            "test-client",
            "test-secret",
            NullLogger<OAuthTokenProvider>.Instance,
            handler,
            TimeProvider.System);
        _initialBinding = (await ((ITokenCacheBindingProvider)_oauth).GetCurrentCacheBinding(default))!;
        var interleaved = new InterleavingTokenProvider(_oauth)
        {
            AfterAcquisition = async () =>
            {
                _address = new("server-b.example");
                handler.AccessToken = "token-b";
                await _oauth.Refresh();
            }
        };
        _provider = new FileSystemCachingTokenProvider(interleaved, _cacheFile);
    }

    protected void ReadPersistedSnapshot()
    {
        using var cache = JsonDocument.Parse(File.ReadAllText(_cacheFile));
        _persistedToken = cache.RootElement.GetProperty("AccessToken").GetString()!;
        _persistedEndpoint = cache.RootElement.GetProperty("Binding").GetProperty("TokenEndpoint").GetString()!;
    }
}
