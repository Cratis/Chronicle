// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ITokenProvider"/> that uses OAuth client credentials flow.
/// </summary>
public class OAuthTokenProvider : ITokenProvider, IDisposable
{
    readonly Func<string> _tokenEndpoint;
    readonly string _clientId;
    readonly string _clientSecret;
    readonly ILogger<OAuthTokenProvider> _logger;
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    readonly HttpClient _httpClient;
    readonly HttpMessageHandler _httpMessageHandler;
    string? _accessToken;
    DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddress">The Chronicle server address.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="disableTls">Whether to disable TLS for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    public OAuthTokenProvider(
        ChronicleServerAddress serverAddress,
        string clientId,
        string clientSecret,
        bool disableTls,
        ILogger<OAuthTokenProvider> logger)
        : this(() => serverAddress, clientId, clientSecret, disableTls, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddressProvider">Provides the Chronicle server address to request tokens from - evaluated per request so it follows the currently connected server.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="disableTls">Whether to disable TLS for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    public OAuthTokenProvider(
        Func<ChronicleServerAddress> serverAddressProvider,
        string clientId,
        string clientSecret,
        bool disableTls,
        ILogger<OAuthTokenProvider> logger)
    {
        var scheme = disableTls ? "http" : "https";
        _tokenEndpoint = () =>
        {
            var serverAddress = serverAddressProvider();
            return $"{scheme}://{serverAddress.Host}:{serverAddress.Port}/connect/token";
        };
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;

        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    // Accept self-signed certificates in development
                    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
                        chain?.ChainStatus.All(status =>
                            status.Status is X509ChainStatusFlags.PartialChain or X509ChainStatusFlags.UntrustedRoot) == true)
                    {
                        _logger.AcceptingSelfSignedCertificate(certificate?.Subject ?? "unknown");
                        return true;
                    }

                    // Accept localhost certificates with name mismatch (for development)
                    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
                    {
                        return true;
                    }

                    _logger.CertificateValidationFailed(sslPolicyErrors.ToString());
                    return false;
                }
            }
        };
        _httpMessageHandler = handler;
        _httpClient = new HttpClient(handler);

        // Deliberately no eager _tokenEndpoint() evaluation here - the provider can be constructed
        // inside the DI factory that also produces the connection the endpoint follows, and
        // resolving it during construction would re-enter that factory.
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
        var tokenEndpoint = _tokenEndpoint();
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                _logger.UsingCachedToken(_tokenExpiry);
                return _accessToken;
            }

            _logger.RequestingAccessToken(tokenEndpoint);

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            });

            var response = await _httpClient.PostAsync(new Uri(tokenEndpoint), content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.TokenRequestFailed((int)response.StatusCode, response.ReasonPhrase ?? "Unknown", errorContent);
                response.EnsureSuccessStatusCode();
                _httpMessageHandler.Dispose();
            }

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
            _logger.FailedToObtainAccessToken(tokenEndpoint, ex);
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.RefreshingAccessToken();
            _accessToken = null;
            _tokenExpiry = DateTimeOffset.MinValue;
        }
        finally
        {
            _refreshLock.Release();
        }

        return await GetAccessToken(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshLock.Dispose();
        _httpMessageHandler.Dispose();
    }
}
