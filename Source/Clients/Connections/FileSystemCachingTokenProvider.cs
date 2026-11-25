// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
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
/// invocation the cached token is returned directly if it has not expired and its endpoint and client
/// binding can be verified against the inner provider. Providers without a known binding do not reuse
/// or write persisted tokens. Old caches without a binding are discarded on the next acquisition.
/// </para>
/// <para>
/// The cache file holds a bearer token, so it is created readable and writable by the current user only
/// (mode 0600 on Unix, in a directory created with mode 0700) and replaced atomically, so a reader never
/// sees a partial file. The location is whatever path the caller supplies for the cache file, which should be
/// inside the user's own profile and unique per server and client identity. The scoped constructor
/// adds a caller-supplied scope to the endpoint and client binding. On Windows the directory and temporary file are created
/// with protected current-user-only access control lists. A JWT's expiry less a safety margin can shorten,
/// but never extend, the configured lifetime.
/// </para>
/// </remarks>
/// <param name="innerProvider">The <see cref="ITokenProvider"/> that performs the actual token acquisition.</param>
/// <param name="cacheFilePath">Path to the file used for persisting the cached token.</param>
/// <param name="tokenLifetime">
/// The maximum time a cached token is considered valid. Defaults to 55 minutes,
/// which is a safe margin for the typical 1-hour token lifetime returned by OAuth servers.
/// </param>
public class FileSystemCachingTokenProvider(
    ITokenProvider innerProvider,
    string cacheFilePath,
    TimeSpan? tokenLifetime = null) : ITokenProvider, IDisposable
{
    static readonly TimeSpan _expirySkew = TimeSpan.FromSeconds(30);
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    static readonly SemaphoreSlim[] _fileLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    readonly TimeSpan _tokenLifetime = tokenLifetime ?? TimeSpan.FromMinutes(55);
    readonly string? _scopeHash;
    readonly uint _lockIndex = LockIndexFor(cacheFilePath);

    /// <summary>
    /// Initializes a token cache bound to a caller-supplied server and client identity scope.
    /// </summary>
    /// <param name="innerProvider">The provider that acquires tokens.</param>
    /// <param name="cacheFilePath">The cache file path, unique per server and client identity.</param>
    /// <param name="tokenLifetime">The maximum cache lifetime; defaults to 55 minutes.</param>
    /// <param name="cacheScope">The cache scope, for example the server address combined with the client id. Only its SHA-256 hash is persisted.</param>
    public FileSystemCachingTokenProvider(ITokenProvider innerProvider, string cacheFilePath, TimeSpan? tokenLifetime, string cacheScope)
        : this(innerProvider, cacheFilePath, tokenLifetime)
    {
        _scopeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheScope)));
    }

    SemaphoreSlim Lock => _fileLocks[_lockIndex];

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var cached = ReadCache(await CurrentBinding(cancellationToken));
        if (cached is not null && DateTimeOffset.UtcNow < cached.Expiry)
        {
            return cached.AccessToken;
        }

        await Lock.WaitAsync(cancellationToken);
        try
        {
            cached = ReadCache(await CurrentBinding(cancellationToken));
            if (cached is not null && DateTimeOffset.UtcNow < cached.Expiry)
            {
                return cached.AccessToken;
            }

            return CacheToken(await AcquireToken(cancellationToken));
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            DeleteCache();
            return CacheToken(await RefreshToken(cancellationToken));
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (innerProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    static bool HasKnownBinding(TokenCacheBinding? binding) => binding?.IsKnown() == true;

    static uint LockIndexFor(string path)
    {
        // Bounded process-wide stripes also serialize separate providers sharing a cache path.
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return (uint)comparer.GetHashCode(Path.GetFullPath(path)) % (uint)_fileLocks.Length;
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

    static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // An optional cache must not prevent token acquisition or refresh.
        }
    }

    DateTimeOffset ExpiryOf(string token)
    {
        var expiry = DateTimeOffset.UtcNow.Add(_tokenLifetime);
        return IssuerExpiryOf(token) is { } issuerExpiry && issuerExpiry - _expirySkew < expiry
            ? issuerExpiry - _expirySkew
            : expiry;
    }

    async Task<TokenWithBinding?> AcquireToken(CancellationToken cancellationToken)
    {
        if (innerProvider is ITokenCacheBindingProvider provider)
        {
            return await provider.GetTokenWithBinding(cancellationToken);
        }

        var token = await innerProvider.GetAccessToken(cancellationToken);
        return token is null ? null : new(token, null);
    }

    async Task<TokenWithBinding?> RefreshToken(CancellationToken cancellationToken)
    {
        if (innerProvider is ITokenCacheBindingProvider provider)
        {
            return await provider.RefreshTokenWithBinding(cancellationToken);
        }

        var token = await innerProvider.Refresh(cancellationToken);
        return token is null ? null : new(token, null);
    }

    string? CacheToken(TokenWithBinding? token)
    {
        if (token is not null)
        {
            WriteCache(new TokenCache(token.AccessToken, ExpiryOf(token.AccessToken), _scopeHash, token.Binding));
        }

        return token?.AccessToken;
    }

    ValueTask<TokenCacheBinding?> CurrentBinding(CancellationToken cancellationToken) =>
        innerProvider is ITokenCacheBindingProvider provider
            ? provider.GetCurrentCacheBinding(cancellationToken)
            : ValueTask.FromResult<TokenCacheBinding?>(null);

    bool MatchesBinding(TokenCache cache, TokenCacheBinding? binding) =>
        cache.Binding == binding && (_scopeHash is null || cache.ScopeHash == _scopeHash);

    TokenCache? ReadCache(TokenCacheBinding? binding)
    {
        if (!HasKnownBinding(binding) || !File.Exists(cacheFilePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(cacheFilePath);
            if (!TokenCacheFileSecurity.CanRead(stream.SafeFileHandle))
            {
                return null;
            }

            var cache = JsonSerializer.Deserialize<TokenCache>(stream, _jsonOptions);
            return cache is { AccessToken.Length: > 0 } && MatchesBinding(cache, binding) ? cache : null;
        }
        catch
        {
            return null;
        }
    }

    void WriteCache(TokenCache cache)
    {
        if (!HasKnownBinding(cache.Binding))
        {
            return;
        }

        var temporaryPath = $"{cacheFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                TokenCacheFileSecurity.CreateDirectory(directory);
            }

            using (var stream = TokenCacheFileSecurity.CreateFile(temporaryPath))
            {
                stream.Write(JsonSerializer.SerializeToUtf8Bytes(cache, _jsonOptions));
            }

            File.Move(temporaryPath, cacheFilePath, overwrite: true);
        }
        catch
        {
            // Return the acquired token even when the optional cache is unavailable.
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    void DeleteCache() => TryDeleteFile(cacheFilePath);

    record TokenCache(string AccessToken, DateTimeOffset Expiry, string? ScopeHash = null, TokenCacheBinding? Binding = null);
}
