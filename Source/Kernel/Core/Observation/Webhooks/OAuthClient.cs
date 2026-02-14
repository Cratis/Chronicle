// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IOAuthClient"/>.
/// </summary>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
[Singleton]
public class OAuthClient(IHttpClientFactory httpClientFactory) : IOAuthClient
{
    /// <inheritdoc/>
    public async Task<AccessTokenInfo> AcquireToken(OAuthAuthorization authorization)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var authority = authorization.Authority.Value.TrimEnd('/');
        var discoveryEndpoint = $"{authority}/.well-known/openid-configuration";

        var configuration = await httpClient.GetFromJsonAsync<OpenIdConfiguration>(discoveryEndpoint);
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
        {
            return AccessTokenInfo.Empty;
        }

        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = authorization.ClientId.Value,
            ["client_secret"] = authorization.ClientSecret.Value
        };

        using var content = new FormUrlEncodedContent(request);
        var response = await httpClient.PostAsync(configuration.TokenEndpoint, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return AccessTokenInfo.Empty;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        return new AccessTokenInfo(tokenResponse.AccessToken, expiresAt);
    }

    record OpenIdConfiguration(
        [property: JsonPropertyName("token_endpoint")] string? TokenEndpoint);

    record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
