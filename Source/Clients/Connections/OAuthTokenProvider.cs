// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ITokenProvider"/> that uses OAuth client credentials flow.
/// </summary>
/// <remarks>
/// The token is cached and proactively refreshed ahead of its actual expiry. When a refresh
/// attempt fails while the cached token is still valid, the cached token keeps being served so a
/// transient token-endpoint outage does not interrupt an otherwise healthy session. Failed fetch
/// attempts are throttled so an outage does not turn every RPC into a token request.
/// </remarks>
public class OAuthTokenProvider : ITokenProvider, IDisposable
{
    /// <summary>
    /// Assumed token lifetime in seconds when the token response carries no expires_in.
    /// </summary>
    const int DefaultExpiresInSeconds = 3600;

    static readonly TimeSpan _refreshMargin = TimeSpan.FromSeconds(60);
    static readonly TimeSpan _failedFetchRetryDelay = TimeSpan.FromSeconds(5);

    readonly Func<string> _tokenEndpoint;
    readonly string _clientId;
    readonly string _clientSecret;
    readonly ILogger<OAuthTokenProvider> _logger;
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    readonly HttpClient _httpClient;
    readonly HttpMessageHandler _httpMessageHandler;
    readonly TimeProvider _timeProvider;
    string? _accessToken;
    DateTimeOffset _refreshAt = DateTimeOffset.MinValue;
    DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    DateTimeOffset _lastFailedFetch = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddress">The Chronicle server address.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    public OAuthTokenProvider(
        ChronicleServerAddress serverAddress,
        string clientId,
        string clientSecret,
        bool skipTlsValidation,
        ILogger<OAuthTokenProvider> logger)
        : this(() => serverAddress, clientId, clientSecret, skipTlsValidation, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddressProvider">Provides the Chronicle server address to request tokens from - evaluated per request so it follows the currently connected server.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    public OAuthTokenProvider(
        Func<ChronicleServerAddress> serverAddressProvider,
        string clientId,
        string clientSecret,
        bool skipTlsValidation,
        ILogger<OAuthTokenProvider> logger)
        : this(serverAddressProvider, clientId, clientSecret, logger, CreateHttpMessageHandler(skipTlsValidation), TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class.
    /// </summary>
    /// <param name="serverAddressProvider">Provides the Chronicle server address to request tokens from - evaluated per request so it follows the currently connected server.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="httpMessageHandler">The <see cref="HttpMessageHandler"/> to perform token requests with.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> for expiry and throttling decisions.</param>
    internal OAuthTokenProvider(
        Func<ChronicleServerAddress> serverAddressProvider,
        string clientId,
        string clientSecret,
        ILogger<OAuthTokenProvider> logger,
        HttpMessageHandler httpMessageHandler,
        TimeProvider timeProvider)
    {
        _tokenEndpoint = () =>
        {
            var serverAddress = serverAddressProvider();
            return $"https://{serverAddress.Host}:{serverAddress.Port}/connect/token";
        };
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;
        _httpMessageHandler = httpMessageHandler;
        _httpClient = new HttpClient(httpMessageHandler);
        _timeProvider = timeProvider;

        // Deliberately no eager _tokenEndpoint() evaluation here - the provider can be constructed
        // inside the DI factory that also produces the connection the endpoint follows, and
        // resolving it during construction would re-enter that factory.
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        if (HasFreshToken())
        {
            _logger.UsingCachedToken(_expiresAt);
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasFreshToken())
            {
                _logger.UsingCachedToken(_expiresAt);
                return _accessToken;
            }

            if (IsThrottled())
            {
                _logger.ThrottlingTokenFetch();
                return CachedTokenIfStillValid();
            }

            return await FetchToken(cancellationToken);
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
            _refreshAt = DateTimeOffset.MinValue;
            _expiresAt = DateTimeOffset.MinValue;

            // A forced refresh is an explicit, evidence-driven request - the server rejected the
            // current token - so it bypasses the failed-fetch throttle.
            _lastFailedFetch = DateTimeOffset.MinValue;
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

    static SocketsHttpHandler CreateHttpMessageHandler(bool skipTlsValidation) => new()
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback =
                CertificateLoader.CreateServerCertificateValidationCallback(skipTlsValidation, pinnedCertificateHash: null)
        }
    };

    async Task<string?> FetchToken(CancellationToken cancellationToken)
    {
        var tokenEndpoint = _tokenEndpoint();
        try
        {
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
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            _accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expiresInProp) ? expiresInProp.GetInt32() : DefaultExpiresInSeconds;

            _expiresAt = _timeProvider.GetUtcNow().AddSeconds(expiresIn);
            _refreshAt = _expiresAt - _refreshMargin;
            _lastFailedFetch = DateTimeOffset.MinValue;

            _logger.ObtainedAccessToken(expiresIn);

            return _accessToken;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _lastFailedFetch = _timeProvider.GetUtcNow();

            if (CachedTokenIfStillValid() is { } cachedToken)
            {
                // Only the refresh margin has been crossed, not the actual expiry - a transient
                // token-endpoint failure must not take down an otherwise healthy session.
                _logger.ServingCachedTokenAfterFailedFetch(tokenEndpoint, _expiresAt, ex);
                return cachedToken;
            }

            _logger.FailedToObtainAccessToken(tokenEndpoint, ex);
            throw;
        }
    }

    bool HasFreshToken() => !string.IsNullOrEmpty(_accessToken) && _timeProvider.GetUtcNow() < _refreshAt;

    bool IsThrottled() => _timeProvider.GetUtcNow() - _lastFailedFetch < _failedFetchRetryDelay;

    string? CachedTokenIfStillValid() =>
        !string.IsNullOrEmpty(_accessToken) && _timeProvider.GetUtcNow() < _expiresAt ? _accessToken : null;
}
