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
/// <para>
/// Wraps an inner <see cref="ITokenProvider"/> and caches its result on disk. On the next
/// invocation the cached token is returned directly if it has not expired, avoiding any
/// network round-trips to the token endpoint.
/// </para>
/// <para>
/// The cache file holds a bearer token, so it is created readable and writable by the current user only
/// (mode 0600 on Unix, in a directory created with mode 0700) and replaced atomically, so a reader never
/// sees a partial file. The location is whatever path the caller supplies for the cache file, which should be
/// inside the user's own profile. When the token is a JWT carrying an expiry claim, that issuer
/// supplied expiry is used; otherwise the configured lifetime applies.
/// </para>
/// </remarks>
/// <param name="innerProvider">The <see cref="ITokenProvider"/> that performs the actual token acquisition.</param>
/// <param name="cacheFilePath">Path to the file used for persisting the cached token.</param>
/// <param name="tokenLifetime">
/// How long a cached token is considered valid when the token carries no expiry of its own. Defaults to 55 minutes,
/// which is a safe margin for the typical 1-hour token lifetime returned by OAuth servers.
/// </param>
public class FileSystemCachingTokenProvider(
    ITokenProvider innerProvider,
    string cacheFilePath,
    TimeSpan? tokenLifetime = null) : ITokenProvider, IDisposable
{
    static readonly TimeSpan _expirySkew = TimeSpan.FromSeconds(30);
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
                WriteCache(new TokenCache(token, ExpiryOf(token)));
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
            WriteCache(new TokenCache(token, ExpiryOf(token)));
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

    static DateTimeOffset? IssuerExpiryOf(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return document.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
        }
        catch
        {
            return null;
        }
    }

    DateTimeOffset ExpiryOf(string token) =>
        IssuerExpiryOf(token) is { } issuerExpiry
            ? issuerExpiry - _expirySkew
            : DateTimeOffset.UtcNow.Add(_tokenLifetime);

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
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(directory);
            }
            else
            {
                Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        var temporaryPath = $"{cacheFilePath}.{Guid.NewGuid():N}.tmp";
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        try
        {
            using (var stream = new FileStream(temporaryPath, options))
            {
                stream.Write(JsonSerializer.SerializeToUtf8Bytes(cache, _jsonOptions));
            }

            File.Move(temporaryPath, cacheFilePath, overwrite: true);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
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
