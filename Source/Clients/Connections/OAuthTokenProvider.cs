// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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
public class OAuthTokenProvider : ITokenProvider, IDisposable, ITokenCacheBindingProvider
{
    /// <summary>
    /// Assumed token lifetime in seconds when the token response carries no expires_in.
    /// </summary>
    const int DefaultExpiresInSeconds = 3600;

    static readonly TimeSpan _refreshMargin = TimeSpan.FromSeconds(60);
    static readonly TimeSpan _failedFetchRetryDelay = TimeSpan.FromSeconds(5);

    readonly Func<string> _tokenEndpoint;
    readonly TokenEndpointSource _source;
    readonly string _clientId;
    readonly string _clientSecret;
    readonly string? _scope;
    readonly ILogger<OAuthTokenProvider> _logger;
    readonly SemaphoreSlim _refreshLock = new(1, 1);
    readonly HttpClient _httpClient;
    readonly HttpMessageHandler _httpMessageHandler;
    readonly TimeProvider _timeProvider;
    volatile CachedToken _cached = CachedToken.None;
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
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class that validates the server certificate against a set of certificate authorities.
    /// </summary>
    /// <param name="serverAddress">The Chronicle server address.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="certificateAuthorities">Certificate authorities the server certificate must chain to when validation is enabled; <see langword="null"/> uses the system trust store.</param>
    public OAuthTokenProvider(
        ChronicleServerAddress serverAddress,
        string clientId,
        string clientSecret,
        bool skipTlsValidation,
        ILogger<OAuthTokenProvider> logger,
        X509Certificate2Collection? certificateAuthorities)
        : this(() => serverAddress, clientId, clientSecret, skipTlsValidation, logger, certificateAuthorities)
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
        : this(serverAddressProvider, clientId, clientSecret, skipTlsValidation, logger, certificateAuthorities: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class that validates the server certificate against a set of certificate authorities.
    /// </summary>
    /// <param name="serverAddressProvider">Provides the Chronicle server address to request tokens from - evaluated per request so it follows the currently connected server.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation for the token request.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="certificateAuthorities">Certificate authorities the server certificate must chain to when validation is enabled; <see langword="null"/> uses the system trust store.</param>
    public OAuthTokenProvider(
        Func<ChronicleServerAddress> serverAddressProvider,
        string clientId,
        string clientSecret,
        bool skipTlsValidation,
        ILogger<OAuthTokenProvider> logger,
        X509Certificate2Collection? certificateAuthorities)
        : this(serverAddressProvider, clientId, clientSecret, logger, CreateHttpMessageHandler(skipTlsValidation, certificateAuthorities), TimeProvider.System)
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
        : this(
            () =>
            {
                var serverAddress = serverAddressProvider();
                return $"https://{serverAddress.Host}:{serverAddress.Port}/connect/token";
            },
            clientId,
            clientSecret,
            null,
            logger,
            httpMessageHandler,
            timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthTokenProvider"/> class that requests tokens from a fixed external endpoint.
    /// </summary>
    /// <param name="tokenEndpoint">The absolute https token endpoint.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="scope">Optional scope to request.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="httpMessageHandler">The <see cref="HttpMessageHandler"/> to perform token requests with.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> for expiry and throttling decisions.</param>
    /// <param name="source">Where the endpoint came from - the only endpoint information diagnostics carry.</param>
    /// <exception cref="InsecureTokenEndpoint">Thrown when the endpoint is not an absolute https URL without user info.</exception>
    internal OAuthTokenProvider(
        Uri tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        ILogger<OAuthTokenProvider> logger,
        HttpMessageHandler httpMessageHandler,
        TimeProvider timeProvider,
        TokenEndpointSource source = TokenEndpointSource.Configured)
        : this(ExternalEndpoint(tokenEndpoint, source), clientId, clientSecret, scope, logger, httpMessageHandler, timeProvider, source)
    {
    }

    OAuthTokenProvider(
        Func<string> tokenEndpoint,
        string clientId,
        string clientSecret,
        string? scope,
        ILogger<OAuthTokenProvider> logger,
        HttpMessageHandler httpMessageHandler,
        TimeProvider timeProvider,
        TokenEndpointSource source = TokenEndpointSource.DefaultServer)
    {
        _tokenEndpoint = tokenEndpoint;
        _source = source;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _logger = logger;
        _httpMessageHandler = httpMessageHandler;
        _httpClient = new HttpClient(httpMessageHandler);
        _timeProvider = timeProvider;

        // Deliberately no eager _tokenEndpoint() evaluation here - the provider can be constructed
        // inside the DI factory that also produces the connection the endpoint follows, and
        // resolving it during construction would re-enter that factory.
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        (await ((ITokenCacheBindingProvider)this).GetTokenWithBinding(cancellationToken))?.AccessToken;

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default) =>
        (await ((ITokenCacheBindingProvider)this).RefreshTokenWithBinding(cancellationToken))?.AccessToken;

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshLock.Dispose();
        _httpMessageHandler.Dispose();
    }

    /// <inheritdoc/>
    async Task<TokenWithBinding?> ITokenCacheBindingProvider.GetTokenWithBinding(CancellationToken cancellationToken)
    {
        // The authentication interceptor reads the token synchronously on every call, including the keep-alive answers
        // that hold the session open. With a fresh token this must complete without touching the refresh lock: an await
        // that does not complete synchronously posts its continuation back to a caller that is blocked waiting for it.
        var binding = CurrentBinding();
        if (TryGetFreshToken(binding) is { } freshToken)
        {
            return freshToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetFreshToken(binding) is { } tokenFromOtherCaller)
            {
                return tokenFromOtherCaller;
            }

            if (_cached.Token is not null && _cached.Token.Binding != binding)
            {
                InvalidateToken();
            }

            if (IsThrottled())
            {
                _logger.ThrottlingTokenFetch();
                return CachedTokenIfStillValid(binding);
            }

            return await FetchToken(binding, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc/>
    async Task<TokenWithBinding?> ITokenCacheBindingProvider.RefreshTokenWithBinding(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.RefreshingAccessToken();

            // A forced refresh bypasses the failed-fetch throttle as well as the cached token.
            InvalidateToken();
        }
        finally
        {
            _refreshLock.Release();
        }

        return await ((ITokenCacheBindingProvider)this).GetTokenWithBinding(cancellationToken);
    }

    /// <inheritdoc/>
    ValueTask<TokenCacheBinding?> ITokenCacheBindingProvider.GetCurrentCacheBinding(CancellationToken cancellationToken) =>
        ValueTask.FromResult<TokenCacheBinding?>(CurrentBinding());

    /// <summary>
    /// Creates a <see cref="HttpMessageHandler"/> for external authorities that always validates the server certificate.
    /// </summary>
    /// <returns>The <see cref="HttpMessageHandler"/>.</returns>
    /// <remarks>
    /// Redirects are never followed: a redirect would resend the client credentials to a host nobody configured.
    /// </remarks>
    internal static SocketsHttpHandler CreateExternalHttpMessageHandler() => new() { AllowAutoRedirect = false };

    /// <summary>
    /// Gets the <see cref="HttpMessageHandler"/> token requests are performed with.
    /// </summary>
    /// <returns>The <see cref="HttpMessageHandler"/>.</returns>
    internal HttpMessageHandler GetMessageHandler() => _httpMessageHandler;

    /// <summary>
    /// Gets the token endpoint tokens are currently requested from.
    /// </summary>
    /// <returns>The token endpoint.</returns>
    internal string GetCurrentTokenEndpoint() => _tokenEndpoint();

    static Func<string> ExternalEndpoint(Uri tokenEndpoint, TokenEndpointSource source)
    {
        var endpoint = ExternalTokenEndpoints.Require(tokenEndpoint, source).AbsoluteUri;
        return () => endpoint;
    }

    static SocketsHttpHandler CreateHttpMessageHandler(bool skipTlsValidation, X509Certificate2Collection? certificateAuthorities) => new()
    {
        AllowAutoRedirect = false,
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback =
                CertificateLoader.CreateServerCertificateValidationCallback(skipTlsValidation, pinnedCertificateHash: null, certificateAuthorities)
        }
    };

    static bool IsTransportFailure(Exception exception) => exception is HttpRequestException { StatusCode: null };

    static bool IsTransientStatus(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 || statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;

    async Task<TokenWithBinding?> FetchToken(TokenCacheBinding binding, CancellationToken cancellationToken)
    {
        var loggedEndpoint = TokenDiagnostics.Label(_source);
        try
        {
            _logger.RequestingAccessToken(loggedEndpoint);

            using var content = new FormUrlEncodedContent(CreateTokenRequestParameters());

            var response = await _httpClient.PostAsync(new Uri(binding.TokenEndpoint), content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await LogTokenRequestFailure(response, loggedEndpoint, cancellationToken);
                throw new HttpRequestException($"The token endpoint returned HTTP status {(int)response.StatusCode}.", null, response.StatusCode);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expiresInProp) ? expiresInProp.GetInt32() : DefaultExpiresInSeconds;

            var expiresAt = _timeProvider.GetUtcNow().AddSeconds(expiresIn);
            var token = accessToken is null ? null : new TokenWithBinding(accessToken, binding);
            _cached = new(token, expiresAt - _refreshMargin, expiresAt);
            _lastFailedFetch = DateTimeOffset.MinValue;

            _logger.ObtainedAccessToken(expiresIn);

            return token;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _lastFailedFetch = _timeProvider.GetUtcNow();

            if (CachedTokenIfStillValid(CurrentBinding()) is { } cachedToken)
            {
                // Only the refresh margin has been crossed, not the actual expiry - a transient
                // token-endpoint failure must not take down an otherwise healthy session.
                _logger.ServingCachedTokenAfterFailedFetch(loggedEndpoint, _cached.ExpiresAt, TokenDiagnostics.Label(TokenDiagnostics.Categorize(ex)));
                return cachedToken;
            }

            LogTokenFetchFailure(loggedEndpoint, ex);
            if (IsTransportFailure(ex))
            {
                // The original transport failure can name the remote host; surface the category only.
                throw new HttpRequestException(TokenDiagnostics.Describe(TokenDiagnostics.Categorize(ex)));
            }

            throw;
        }
    }

    async Task LogTokenRequestFailure(HttpResponseMessage response, string loggedEndpoint, CancellationToken cancellationToken)
    {
        // Never log the remote reason phrase or response body: a token endpoint can reflect the submitted credentials.
        var errorCode = OAuthErrorCodes.Describe(await response.Content.ReadAsStringAsync(cancellationToken));
        var statusName = response.StatusCode.ToString();
        if (IsTransientStatus(response.StatusCode))
        {
            _logger.TokenRequestFailed(loggedEndpoint, (int)response.StatusCode, statusName, errorCode);
        }
        else
        {
            _logger.TokenRequestRejected(loggedEndpoint, (int)response.StatusCode, statusName, errorCode);
        }
    }

    void LogTokenFetchFailure(string loggedEndpoint, Exception exception)
    {
        switch (exception)
        {
            case HttpRequestException { StatusCode: not null }:
                // The HTTP response status was already reported; do not log it a second time.
                break;
            case HttpRequestException:
                _logger.TokenEndpointUnreachable(loggedEndpoint, TokenDiagnostics.Label(TokenDiagnostics.Categorize(exception)));
                break;
            default:
                _logger.FailedToObtainAccessToken(loggedEndpoint, TokenDiagnostics.Label(TokenDiagnostics.Categorize(exception)));
                break;
        }
    }

    TokenCacheBinding CurrentBinding() => new(_tokenEndpoint(), _clientId);

    void InvalidateToken()
    {
        _cached = CachedToken.None;
        _lastFailedFetch = DateTimeOffset.MinValue;
    }

    TokenWithBinding? TryGetFreshToken(TokenCacheBinding binding)
    {
        var cached = _cached;
        if (cached.Token is null || string.IsNullOrEmpty(cached.Token.AccessToken) || cached.Token.Binding != binding)
        {
            return null;
        }

        if (_timeProvider.GetUtcNow() >= cached.RefreshAt)
        {
            return null;
        }

        _logger.UsingCachedToken(cached.ExpiresAt);
        return cached.Token;
    }

    bool IsThrottled() => _timeProvider.GetUtcNow() - _lastFailedFetch < _failedFetchRetryDelay;

    TokenWithBinding? CachedTokenIfStillValid(TokenCacheBinding binding)
    {
        var cached = _cached;
        return !string.IsNullOrEmpty(cached.Token?.AccessToken) && cached.Token.Binding == binding && _timeProvider.GetUtcNow() < cached.ExpiresAt
            ? cached.Token
            : null;
    }

    Dictionary<string, string> CreateTokenRequestParameters()
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        if (!string.IsNullOrWhiteSpace(_scope))
        {
            parameters["scope"] = _scope;
        }

        return parameters;
    }

    /// <summary>
    /// The token and its refresh/expiry times, published as one immutable snapshot so the hot path reads them
    /// without the refresh lock and never observes a half-updated token.
    /// </summary>
    /// <param name="Token">The token, if any.</param>
    /// <param name="RefreshAt">When the token should be replaced.</param>
    /// <param name="ExpiresAt">When the token stops being usable.</param>
    sealed record CachedToken(TokenWithBinding? Token, DateTimeOffset RefreshAt, DateTimeOffset ExpiresAt)
    {
        public static readonly CachedToken None = new(null, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
    }
}
