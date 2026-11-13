// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider.given;

public class a_caching_provider : Specification
{
    protected ITokenProvider _inner;
    protected FileSystemCachingTokenProvider _provider;
    protected string _directory;
    protected string _cacheFile;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _cacheFile = Path.Combine(_directory, "nested", "token.json");
        _inner = Substitute.For<ITokenProvider>();
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

    protected static string JwtExpiringIn(TimeSpan lifetime)
    {
        static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        return $"{Encode("{\"alg\":\"none\"}")}.{Encode($"{{\"exp\":{exp}}}")}.signature";
    }
}
