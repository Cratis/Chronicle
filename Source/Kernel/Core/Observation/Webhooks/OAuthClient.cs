// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Acquires and shares client-credentials tokens until shortly before their expiry.
/// </summary>
/// <param name="httpClientFactory">The HTTP client factory.</param>
/// <param name="timeProvider">The clock used for token expiry.</param>
[Singleton]
public class OAuthClient(IHttpClientFactory httpClientFactory, TimeProvider? timeProvider = null) : IOAuthClient
{
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _endpoints = new();
    readonly ConcurrentDictionary<OAuthTokenKey, Lazy<Task<AccessTokenInfo>>> _tokens = new();

    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization) => AcquireToken(authorization, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, CancellationToken cancellationToken)
    {
        var authority = authorization.Authority.Value.TrimEnd('/');
        var discovery = _endpoints.GetOrAdd(authority, static (key, owner) => new(() => owner.Discover(key)), this);
        string? endpoint;
        try
        {
            endpoint = await discovery.Value.WaitAsync(cancellationToken);
        }
        catch when (discovery.IsValueCreated && discovery.Value.IsFaulted)
        {
            _endpoints.TryRemove(new(authority, discovery));
            throw;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _endpoints.TryRemove(new(authority, discovery));
            return AccessTokenInfo.Empty;
        }

        var key = Key(endpoint, authorization);
        while (true)
        {
            var request = _tokens.GetOrAdd(key, static (_, state) => new(() => state.Client.Fetch(state.Endpoint, state.Authorization)), (Client: this, Endpoint: endpoint, Authorization: authorization));
            if (request.IsValueCreated && request.Value.IsCompletedSuccessfully && _clock.GetUtcNow() >= (await request.Value).ExpiresAt)
            {
                _tokens.TryRemove(new(key, request));
                continue;
            }

            var task = request.Value;
            try
            {
                var token = await task.WaitAsync(cancellationToken);
                if (token.ExpiresAt <= _clock.GetUtcNow())
                {
                    _tokens.TryRemove(new(key, request));
                }
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
    public void Invalidate(OAuthAuthorization authorization, AccessTokenInfo token)
    {
        var authority = authorization.Authority.Value.TrimEnd('/');
        if (_endpoints.TryGetValue(authority, out var discovery) && discovery.IsValueCreated && discovery.Value.IsCompletedSuccessfully && discovery.Value.Result is { } endpoint)
        {
            var key = Key(endpoint, authorization);
            if (_tokens.TryGetValue(key, out var request) && request.IsValueCreated && request.Value.IsCompletedSuccessfully && ReferenceEquals(request.Value.Result, token))
            {
                _tokens.TryRemove(new(key, request));
            }
        }
    }

    static OAuthTokenKey Key(string endpoint, OAuthAuthorization authorization) => new(
        endpoint,
        authorization.ClientId.Value,
        authorization.Scope,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization.ClientSecret.Value))));

    async Task<string?> Discover(string authority)
    {
        using var client = httpClientFactory.CreateClient();
        var configuration = await client.GetFromJsonAsync<OpenIdConfiguration>($"{authority}/.well-known/openid-configuration");
        return configuration?.TokenEndpoint;
    }

    async Task<AccessTokenInfo> Fetch(string endpoint, OAuthAuthorization authorization)
    {
        using var client = httpClientFactory.CreateClient();
        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = authorization.ClientId.Value,
            ["client_secret"] = authorization.ClientSecret.Value
        };
        if (!string.IsNullOrEmpty(authorization.Scope))
        {
            request["scope"] = authorization.Scope;
        }

        using var content = new FormUrlEncodedContent(request);
        using var response = await client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            return AccessTokenInfo.Empty;
        }

        var lifetime = Math.Max(0, token.ExpiresIn);
        var margin = Math.Min(60, lifetime / 2.0);
        return new(token.AccessToken, _clock.GetUtcNow().AddSeconds(lifetime - margin));
    }

    record OAuthTokenKey(string Endpoint, string ClientId, string Scope, string SecretHash);
    record OpenIdConfiguration([property: JsonPropertyName("token_endpoint")] string? TokenEndpoint);
    record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
