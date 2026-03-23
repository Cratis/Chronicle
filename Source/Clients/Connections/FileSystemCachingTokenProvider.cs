// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ITokenProvider"/> that persists the token to disk
/// between process invocations, enabling short-lived clients such as CLIs to reuse tokens
/// without authenticating on every command.
/// </summary>
/// <remarks>
/// Wraps an inner <see cref="ITokenProvider"/> and caches its result on disk. On the next
/// invocation the cached token is returned directly if it has not expired, avoiding any
/// network round-trips to the token endpoint.
/// </remarks>
/// <param name="innerProvider">The <see cref="ITokenProvider"/> that performs the actual token acquisition.</param>
/// <param name="cacheFilePath">Path to the file used for persisting the cached token.</param>
/// <param name="tokenLifetime">
/// How long a cached token is considered valid. Defaults to 55 minutes, which is a safe
/// margin for the typical 1-hour token lifetime returned by OAuth servers.
/// </param>
public class FileSystemCachingTokenProvider(
    ITokenProvider innerProvider,
    string cacheFilePath,
    TimeSpan? tokenLifetime = null) : ITokenProvider, IDisposable
{
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    readonly TimeSpan _tokenLifetime = tokenLifetime ?? TimeSpan.FromMinutes(55);
    readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var cached = ReadCache();
        if (cached is not null && DateTimeOffset.UtcNow < cached.Expiry)
        {
            return cached.AccessToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            cached = ReadCache();
            if (cached is not null && DateTimeOffset.UtcNow < cached.Expiry)
            {
                return cached.AccessToken;
            }

            var token = await innerProvider.GetAccessToken(cancellationToken);
            if (token is not null)
            {
                WriteCache(new TokenCache(token, DateTimeOffset.UtcNow.Add(_tokenLifetime)));
            }

            return token;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default)
    {
        DeleteCache();
        var token = await innerProvider.Refresh(cancellationToken);
        if (token is not null)
        {
            WriteCache(new TokenCache(token, DateTimeOffset.UtcNow.Add(_tokenLifetime)));
        }

        return token;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _lock.Dispose();
        if (innerProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    TokenCache? ReadCache()
    {
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(cacheFilePath);
            return JsonSerializer.Deserialize<TokenCache>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    void WriteCache(TokenCache cache)
    {
        var directory = Path.GetDirectoryName(cacheFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(cacheFilePath, JsonSerializer.Serialize(cache, _jsonOptions));
    }

    void DeleteCache()
    {
        if (File.Exists(cacheFilePath))
        {
            File.Delete(cacheFilePath);
        }
    }

    record TokenCache(string AccessToken, DateTimeOffset Expiry);
}
