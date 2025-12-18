// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ITokenProvider"/> that uses OAuth client credentials flow.
/// </summary>
public class OAuthTokenProvider : ITokenProvider, IDisposable
{
    readonly string _tokenEndpoint;
    readonly string _clientId;
    readonly string _clientSecret;
    readonly ILogger<OAuthTokenProvider> _logger;
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    readonly HttpClient _httpClient = new();
    string? _accessToken;
    DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddress">The Chronicle server address.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="logger">Logger for logging.</param>
    public OAuthTokenProvider(
        ChronicleServerAddress serverAddress,
        string clientId,
        string clientSecret,
        ILogger<OAuthTokenProvider> logger)
    {
        _tokenEndpoint = $"http://{serverAddress.Host}:{serverAddress.Port}/connect/token";
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;

        _logger.InitializingTokenProvider(_tokenEndpoint);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            _logger.UsingCachedToken(_tokenExpiry);
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                _logger.UsingCachedToken(_tokenExpiry);
                return _accessToken;
            }

            _logger.RequestingAccessToken(_tokenEndpoint);

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            });

            var response = await _httpClient.PostAsync(_tokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            _accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expiresInProp) ? expiresInProp.GetInt32() : 3600;

            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

            _logger.ObtainedAccessToken(expiresIn);

            return _accessToken;
        }
        catch (Exception ex)
        {
            _logger.FailedToObtainAccessToken(_tokenEndpoint, ex);
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshLock.Dispose();
    }
}
