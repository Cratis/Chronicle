// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider.given;

public class a_caching_provider : Specification
{
    internal TokenCacheBinding _binding;
    protected ITokenProvider _inner;
    protected FileSystemCachingTokenProvider _provider;
    protected string _directory;
    protected string _cacheFile;

    protected string CacheDirectory => Path.GetDirectoryName(_cacheFile)!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _cacheFile = Path.Combine(_directory, "nested", "token.json");
        _inner = Substitute.For<ITokenProvider, ITokenCacheBindingProvider>();
        _binding = new("https://server.example/connect/token", "client-one");
        var bound = (ITokenCacheBindingProvider)_inner;
        bound.GetCurrentCacheBinding(Arg.Any<CancellationToken>()).Returns(_binding);
        bound.GetTokenWithBinding(Arg.Any<CancellationToken>()).Returns(call =>
            WithBinding(_inner.GetAccessToken(call.Arg<CancellationToken>()), _binding));
        bound.RefreshTokenWithBinding(Arg.Any<CancellationToken>()).Returns(call =>
            WithBinding(_inner.Refresh(call.Arg<CancellationToken>()), _binding));
        _provider = new FileSystemCachingTokenProvider(_inner, _cacheFile);
    }

    void Destroy()
    {
        _provider.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    static async Task<TokenWithBinding?> WithBinding(Task<string?> acquisition, TokenCacheBinding binding)
    {
        var token = await acquisition;
        return token is null ? null : new(token, binding);
    }

    protected string CacheProperty(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(_cacheFile));
        return document.RootElement.GetProperty(name).GetString()!;
    }

    protected static string JwtExpiringIn(TimeSpan lifetime)
    {
        static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        return $"{Encode("{\"alg\":\"none\"}")}.{Encode($"{{\"exp\":{exp}}}")}.signature";
    }
}
