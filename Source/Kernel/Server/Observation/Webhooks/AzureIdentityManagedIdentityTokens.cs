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
    readonly ConcurrentDictionary<TokenKey, Lazy<Task<AccessTokenInfo>>> _tokens = new();

    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(ManagedIdentityAuthorization authorization, CancellationToken cancellationToken)
    {
        var key = new TokenKey(authorization.ClientId, authorization.Scope);
        while (true)
        {
            var request = _tokens.GetOrAdd(key, static (_, state) => new(() => state.Self.Fetch(state.Authorization)), (Self: this, Authorization: authorization));
            if (CompletedToken(request) is { } cached && NeedsRefresh(cached))
            {
                _tokens.TryRemove(new(key, request));
                continue;
            }

            var task = request.Value;
            try
            {
                var token = await task.WaitAsync(cancellationToken);
                if (NeedsRefresh(token)) _tokens.TryRemove(new(key, request));
                return token;
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
        if (ReferenceEquals(CompletedToken(request), token)) _tokens.TryRemove(new(key, request));
    }

    static AccessTokenInfo? CompletedToken(Lazy<Task<AccessTokenInfo>> request)
    {
        if (!request.IsValueCreated) return null;
        return request.Value.IsCompletedSuccessfully ? request.Value.Result : null;
    }

    bool NeedsRefresh(AccessTokenInfo token) => _clock.GetUtcNow() >= token.ExpiresAt - _refreshMargin;

    async Task<AccessTokenInfo> Fetch(ManagedIdentityAuthorization authorization)
    {
        var credential = _credentials.GetOrAdd(authorization.ClientId, static (clientId, factory) => new(() => factory.Create(clientId)), credentialFactory).Value;
        var token = await credential.GetTokenAsync(new TokenRequestContext([authorization.Scope.Value]), CancellationToken.None);
        return new(token.Token, token.ExpiresOn);
    }

    record TokenKey(ManagedIdentityClientId ClientId, OAuthScope Scope);
}
