// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IOAuthClient"/>.
/// </summary>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
/// <param name="policy">The target policy.</param>
[Singleton]
public class OAuthClient(IHttpClientFactory httpClientFactory, IWebhookTargetPolicy policy) : IOAuthClient
{
    /// <summary>
    /// The dedicated OAuth client, sharing webhook target validation and redirect protection.
    /// </summary>
    public const string HttpClientName = "webhook-oauth";

    /// <inheritdoc/>
    public Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization) => AcquireToken(authorization, null);

    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization, EventStoreName? eventStore)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        var authority = authorization.Authority.Value.TrimEnd('/');
        var discoveryEndpoint = $"{authority}/.well-known/openid-configuration";

        await Validate(discoveryEndpoint, eventStore);
        using var discoveryRequest = CreateRequest(HttpMethod.Get, discoveryEndpoint, eventStore);
        using var discoveryResponse = await httpClient.SendAsync(discoveryRequest);
        EnsureSuccess(discoveryResponse);
        var configuration = await discoveryResponse.Content.ReadFromJsonAsync<OpenIdConfiguration>();
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
        {
            return AccessTokenInfo.Empty;
        }

        await Validate(configuration.TokenEndpoint, eventStore);

        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = authorization.ClientId.Value,
            ["client_secret"] = authorization.ClientSecret.Value
        };

        using var tokenRequest = CreateRequest(HttpMethod.Post, configuration.TokenEndpoint, eventStore);
        tokenRequest.Content = new FormUrlEncodedContent(request);
        using var response = await httpClient.SendAsync(tokenRequest);
        EnsureSuccess(response);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return AccessTokenInfo.Empty;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        return new AccessTokenInfo(tokenResponse.AccessToken, expiresAt);
    }

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

    async Task Validate(string url, EventStoreName? eventStore)
    {
        var result = await policy.Evaluate(url, eventStore);
        if (!result.IsAllowed) throw new WebhookTargetRejected(result.Reason);
    }

    record OpenIdConfiguration(
        [property: JsonPropertyName("token_endpoint")] string? TokenEndpoint);

    record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
