// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Acquires and shares client-credentials tokens until shortly before their expiry.
/// </summary>
/// <param name="httpClientFactory">The HTTP client factory.</param>
/// <param name="policy">The target policy.</param>
/// <param name="timeProvider">The clock used for token expiry.</param>
/// <param name="httpClientName">The guarded transport client name.</param>
[Singleton]
public class OAuthClient(IHttpClientFactory httpClientFactory, IWebhookTargetPolicy policy, TimeProvider? timeProvider = null, string httpClientName = OAuthClient.HttpClientName) : IOAuthClient
{
    /// <summary>
    /// The dedicated OAuth client, sharing webhook target validation and redirect protection.
    /// </summary>
    public const string HttpClientName = "webhook-oauth";

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _endpoints = new();
    readonly ConcurrentDictionary<OAuthTokenKey, Lazy<Task<AccessTokenInfo>>> _tokens = new();

    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization) => AcquireToken(authorization, null, CancellationToken.None);

    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, EventStoreName? eventStore) => AcquireToken(authorization, eventStore, CancellationToken.None);

    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, CancellationToken cancellationToken) => AcquireToken(authorization, null, cancellationToken);

    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, EventStoreName? eventStore, CancellationToken cancellationToken)
    {
        var authority = authorization.Authority.Value.TrimEnd('/');
        await Validate($"{authority}/.well-known/openid-configuration", eventStore, cancellationToken);
        var discovery = _endpoints.GetOrAdd(authority, static (key, state) => new(() => state.Client.Discover(key, state.EventStore)), (Client: this, EventStore: eventStore));
        string? endpoint;
        try
        {
            endpoint = await discovery.Value.WaitAsync(cancellationToken);
        }
        catch when (discovery.IsValueCreated && (discovery.Value.IsFaulted || discovery.Value.IsCanceled))
        {
            _endpoints.TryRemove(new(authority, discovery));
            throw;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _endpoints.TryRemove(new(authority, discovery));
            return AccessTokenInfo.Empty;
        }

        await Validate(endpoint, eventStore, cancellationToken);
        var key = Key(endpoint, authorization);
        while (true)
        {
            var request = _tokens.GetOrAdd(key, static (_, state) => new(() => state.Client.Fetch(state.Endpoint, state.Authorization, state.EventStore)), (Client: this, Endpoint: endpoint, Authorization: authorization, EventStore: eventStore));
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

    static HttpRequestMessage CreateRequest(HttpMethod method, string url, EventStoreName? eventStore)
    {
        var request = new HttpRequestMessage(method, url);
        if (eventStore is not null) request.Options.Set(WebhookRequestOptions.EventStore, eventStore);
        return request;
    }

    static void EnsureSuccess(HttpResponseMessage response)
    {
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            throw new WebhookTargetRejected($"OAuth redirects are not allowed (HTTP {(int)response.StatusCode}).");
        }

        response.EnsureSuccessStatusCode();
    }

    async Task<string?> Discover(string authority, EventStoreName? eventStore)
    {
        using var client = httpClientFactory.CreateClient(httpClientName);
        using var request = CreateRequest(HttpMethod.Get, $"{authority}/.well-known/openid-configuration", eventStore);
        using var response = await client.SendAsync(request);
        EnsureSuccess(response);
        var configuration = await response.Content.ReadFromJsonAsync<OpenIdConfiguration>();
        return configuration?.TokenEndpoint;
    }

    async Task<AccessTokenInfo> Fetch(string endpoint, OAuthAuthorization authorization, EventStoreName? eventStore)
    {
        using var client = httpClientFactory.CreateClient(httpClientName);
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

        using var tokenRequest = CreateRequest(HttpMethod.Post, endpoint, eventStore);
        tokenRequest.Content = new FormUrlEncodedContent(request);
        using var response = await client.SendAsync(tokenRequest);
        EnsureSuccess(response);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            return AccessTokenInfo.Empty;
        }

        var lifetime = Math.Max(0, token.ExpiresIn);
        var margin = Math.Min(60, lifetime / 2.0);
        return new(token.AccessToken, _clock.GetUtcNow().AddSeconds(lifetime - margin));
    }

    async Task Validate(string url, EventStoreName? eventStore, CancellationToken cancellationToken)
    {
        var result = await policy.EvaluateForDelivery(url, eventStore, cancellationToken);
        if (!result.IsAllowed) throw new WebhookTargetRejected(result.Reason);
    }

    record OAuthTokenKey(string Endpoint, string ClientId, string Scope, string SecretHash);
    record OpenIdConfiguration([property: JsonPropertyName("token_endpoint")] string? TokenEndpoint);
    record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
