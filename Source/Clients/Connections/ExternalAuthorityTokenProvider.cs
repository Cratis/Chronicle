// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ITokenProvider"/> that discovers the token endpoint of an external
/// OAuth/OIDC authority and then uses the client credentials flow against it.
/// </summary>
/// <remarks>
/// Discovery is lazy, happens once and is cached for the life of the provider once it succeeds. A failed
/// discovery is remembered for a short delay, during which the same failure is rethrown without contacting the
/// authority again, so an outage does not turn every RPC into a discovery request. Redirects are never followed.
/// </remarks>
internal sealed class ExternalAuthorityTokenProvider : ITokenProvider, IDisposable
{
    const string DiscoveryPath = "/.well-known/openid-configuration";

    /// <summary>
    /// The longest a discovery request may take. Discovery is one small GET that runs under a lock every token request waits on,
    /// so 15 seconds is generous for a healthy authority while keeping an unreachable one from stalling callers for the 100 second HttpClient default.
    /// </summary>
    static readonly TimeSpan _discoveryTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long a failed discovery is rethrown without contacting the authority again - the same delay <see cref="OAuthTokenProvider"/> applies to failed token fetches.
    /// </summary>
    static readonly TimeSpan _failedDiscoveryRetryDelay = TimeSpan.FromSeconds(5);

    readonly Uri _authority;
    readonly string _clientId;
    readonly string _clientSecret;
    readonly string? _scope;
    readonly ILogger<ExternalAuthorityTokenProvider> _logger;
    readonly ILogger<OAuthTokenProvider> _tokenLogger;
    readonly HttpMessageHandler _handler;
    readonly TimeProvider _timeProvider;
    readonly HttpClient _discoveryClient;
    readonly SemaphoreSlim _discoveryLock = new(1, 1);
    OAuthTokenProvider? _inner;
    TokenFailureCategory? _lastFailure;
    DateTimeOffset _lastFailedDiscovery = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalAuthorityTokenProvider"/> class.
    /// </summary>
    /// <param name="authority">The authority to discover the token endpoint from.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="scope">Optional scope to request.</param>
    /// <param name="logger">Logger for discovery.</param>
    /// <param name="tokenLogger">Logger for the token requests.</param>
    internal ExternalAuthorityTokenProvider(
        Uri authority,
        string clientId,
        string clientSecret,
        string? scope,
        ILogger<ExternalAuthorityTokenProvider> logger,
        ILogger<OAuthTokenProvider> tokenLogger)
        : this(authority, clientId, clientSecret, scope, logger, tokenLogger, OAuthTokenProvider.CreateExternalHttpMessageHandler(), TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalAuthorityTokenProvider"/> class.
    /// </summary>
    /// <param name="authority">The authority to discover the token endpoint from.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="scope">Optional scope to request.</param>
    /// <param name="logger">Logger for discovery.</param>
    /// <param name="tokenLogger">Logger for the token requests.</param>
    /// <param name="handler">The <see cref="HttpMessageHandler"/> for discovery and token requests.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> for expiry and throttling decisions.</param>
    internal ExternalAuthorityTokenProvider(
        Uri authority,
        string clientId,
        string clientSecret,
        string? scope,
        ILogger<ExternalAuthorityTokenProvider> logger,
        ILogger<OAuthTokenProvider> tokenLogger,
        HttpMessageHandler handler,
        TimeProvider timeProvider)
    {
        _authority = ExternalTokenEndpoints.Require(authority, TokenEndpointSource.Authority);
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _logger = logger;
        _tokenLogger = tokenLogger;
        _handler = handler;
        _timeProvider = timeProvider;
        _discoveryClient = new HttpClient(handler, disposeHandler: false) { Timeout = _discoveryTimeout };
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        await (await GetInner(cancellationToken)).GetAccessToken(cancellationToken);

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default) =>
        await (await GetInner(cancellationToken, bypassThrottle: true)).Refresh(cancellationToken);

    /// <inheritdoc/>
    public void Dispose()
    {
        _discoveryClient.Dispose();
        _discoveryLock.Dispose();
        if (_inner is not null)
        {
            _inner.Dispose();
        }
        else
        {
            _handler.Dispose();
        }
    }

    /// <summary>
    /// Gets the <see cref="HttpMessageHandler"/> discovery and token requests are performed with.
    /// </summary>
    /// <returns>The <see cref="HttpMessageHandler"/>.</returns>
    internal HttpMessageHandler GetMessageHandler() => _handler;

    async Task<OAuthTokenProvider> GetInner(CancellationToken cancellationToken, bool bypassThrottle = false)
    {
        await _discoveryLock.WaitAsync(cancellationToken);
        try
        {
            if (_inner is not null)
            {
                return _inner;
            }

            if (_lastFailure is { } failure && !bypassThrottle && _timeProvider.GetUtcNow() - _lastFailedDiscovery < _failedDiscoveryRetryDelay)
            {
                throw new TokenEndpointDiscoveryFailed(failure);
            }

            var endpoint = await Discover(cancellationToken);
            _lastFailure = null;
            _inner = new OAuthTokenProvider(endpoint, _clientId, _clientSecret, _scope, _tokenLogger, _handler, _timeProvider, TokenEndpointSource.Discovered);
            return _inner;
        }
        finally
        {
            _discoveryLock.Release();
        }
    }

    async Task<Uri> Discover(CancellationToken cancellationToken)
    {
        _logger.DiscoveringTokenEndpoint();
        try
        {
            var discoveryUri = new Uri($"{_authority.AbsoluteUri.TrimEnd('/')}{DiscoveryPath}");
            using var response = await _discoveryClient.GetAsync(discoveryUri, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                throw new HttpRequestException($"The authority answered with a redirect ({(int)response.StatusCode}); redirects are not followed", null, response.StatusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"The authority returned HTTP status {(int)response.StatusCode}.", null, response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            string? tokenEndpoint = null;
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("token_endpoint", out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                tokenEndpoint = property.GetString();
            }

            if (!ExternalTokenEndpoints.TryParse(tokenEndpoint, out var endpoint))
            {
                throw new InsecureTokenEndpoint(TokenEndpointSource.Discovered);
            }

            _logger.DiscoveredTokenEndpoint();
            return endpoint;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Includes the HttpClient timeout, which surfaces as a cancellation the caller did not ask for.
            // Only a code-owned category survives: the exception can name the host or carry remote text.
            var category = TokenDiagnostics.Categorize(ex);
            _logger.TokenEndpointDiscoveryFailed(TokenDiagnostics.Label(category));
            _lastFailure = category;
            _lastFailedDiscovery = _timeProvider.GetUtcNow();
            throw new TokenEndpointDiscoveryFailed(category);
        }
    }
}
