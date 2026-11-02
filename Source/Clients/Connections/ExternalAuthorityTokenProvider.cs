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
/// discovery is not cached and is retried on the next request.
/// </remarks>
internal sealed class ExternalAuthorityTokenProvider : ITokenProvider, IDisposable
{
    const string DiscoveryPath = "/.well-known/openid-configuration";

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
        _authority = ExternalTokenEndpoints.Require(authority);
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _logger = logger;
        _tokenLogger = tokenLogger;
        _handler = handler;
        _timeProvider = timeProvider;
        _discoveryClient = new HttpClient(handler, disposeHandler: false);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        await (await GetInner(cancellationToken)).GetAccessToken(cancellationToken);

    /// <inheritdoc/>
    public async Task<string?> Refresh(CancellationToken cancellationToken = default) =>
        await (await GetInner(cancellationToken)).Refresh(cancellationToken);

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

    async Task<OAuthTokenProvider> GetInner(CancellationToken cancellationToken)
    {
        await _discoveryLock.WaitAsync(cancellationToken);
        try
        {
            if (_inner is not null)
            {
                return _inner;
            }

            var endpoint = await Discover(cancellationToken);
            _inner = new OAuthTokenProvider(endpoint, _clientId, _clientSecret, _scope, _tokenLogger, _handler, _timeProvider);
            return _inner;
        }
        finally
        {
            _discoveryLock.Release();
        }
    }

    async Task<Uri> Discover(CancellationToken cancellationToken)
    {
        var authority = ExternalTokenEndpoints.Describe(_authority);
        _logger.DiscoveringTokenEndpoint(authority);
        try
        {
            var discoveryUri = new Uri($"{_authority.AbsoluteUri.TrimEnd('/')}{DiscoveryPath}");
            using var response = await _discoveryClient.GetAsync(discoveryUri, cancellationToken);
            response.EnsureSuccessStatusCode();

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
                throw new InsecureTokenEndpoint(Uri.TryCreate(tokenEndpoint, UriKind.Absolute, out var offending) ? ExternalTokenEndpoints.Describe(offending) : "(missing or invalid token_endpoint)");
            }

            _logger.DiscoveredTokenEndpoint(authority, ExternalTokenEndpoints.Describe(endpoint));
            return endpoint;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.TokenEndpointDiscoveryFailed(authority, ex);
            throw new TokenEndpointDiscoveryFailed(authority, ex);
        }
    }
}
