// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Azure.Core;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Server.Observation.Webhooks;

/// <summary>
/// Shares Azure identity credentials and tokens for webhook delivery without storing secrets.
/// </summary>
/// <param name="credentialFactory">Creates the Azure credentials.</param>
/// <param name="timeProvider">The clock used for token refresh.</param>
[Singleton]
public class AzureIdentityManagedIdentityTokens(IManagedIdentityCredentialFactory credentialFactory, TimeProvider? timeProvider = null) : IManagedIdentityTokens
{
    static readonly TimeSpan _refreshMargin = TimeSpan.FromSeconds(60);
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<ManagedIdentityClientId, Lazy<TokenCredential>> _credentials = new();
    readonly ConcurrentDictionary<TokenKey, Lazy<Task<TokenEntry>>> _tokens = new();

    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(ManagedIdentityAuthorization authorization, CancellationToken cancellationToken)
    {
        var key = new TokenKey(authorization.ClientId, authorization.Scope);
        while (true)
        {
            var request = _tokens.GetOrAdd(key, static (_, state) => new(() => state.Self.Fetch(state.Authorization)), (Self: this, Authorization: authorization));
            if (CompletedToken(request) is { } cached && NeedsRefresh(cached.Token))
            {
                _tokens.TryRemove(new(key, request));
                continue;
            }

            var task = request.Value;
            try
            {
                var token = await task.WaitAsync(cancellationToken);
                if (NeedsRefresh(token.Token)) _tokens.TryRemove(new(key, request));
                return token.Token;
            }
            catch when (task.IsFaulted || task.IsCanceled)
            {
                _tokens.TryRemove(new(key, request));
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public void Invalidate(ManagedIdentityAuthorization authorization, AccessTokenInfo token)
    {
        var key = new TokenKey(authorization.ClientId, authorization.Scope);
        if (!_tokens.TryGetValue(key, out var request)) return;
        var entry = CompletedToken(request);
        if (entry is null || !ReferenceEquals(entry.Token, token)) return;
        if (_tokens.TryRemove(new(key, request)))
        {
            // Azure credentials also cache tokens. Never evict a replacement credential for a stale rejection.
            _credentials.TryRemove(new(key.ClientId, entry.Credential));
        }
    }

    static TokenEntry? CompletedToken(Lazy<Task<TokenEntry>> request)
    {
        if (!request.IsValueCreated) return null;
        return request.Value.IsCompletedSuccessfully ? request.Value.Result : null;
    }

    bool NeedsRefresh(AccessTokenInfo token) => _clock.GetUtcNow() >= token.ExpiresAt - _refreshMargin;

    async Task<TokenEntry> Fetch(ManagedIdentityAuthorization authorization)
    {
        var credential = _credentials.GetOrAdd(authorization.ClientId, static (clientId, factory) => new(() => factory.Create(clientId)), credentialFactory);
        var token = await credential.Value.GetTokenAsync(new TokenRequestContext([authorization.Scope.Value]), CancellationToken.None);
        return new(new(token.Token, token.ExpiresOn), credential);
    }

    record TokenEntry(AccessTokenInfo Token, Lazy<TokenCredential> Credential);
    record TokenKey(ManagedIdentityClientId ClientId, OAuthScope Scope);
}
