// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Applies a current OAuth token to each request and retries a rejected token once.
/// </summary>
/// <param name="client">The transport client, owned by the handler.</param>
/// <param name="authorization">The OAuth configuration.</param>
/// <param name="tokens">The shared token provider.</param>
public class OAuthHttpMessageHandler(HttpClient client, OAuthAuthorization authorization, IOAuthClient tokens) : HttpMessageHandler
{
    readonly HttpMessageInvoker _handler = new(
        new TokenHttpMessageHandler(
            client,
            (eventStore, cancellation) => tokens.AcquireToken(authorization, eventStore, cancellation),
            token => tokens.Invalidate(authorization, token)),
        disposeHandler: true);

    /// <summary>
    /// Wraps a configured transport client, preserving its address, timeout and headers.
    /// </summary>
    /// <param name="client">The configured transport.</param>
    /// <param name="authorization">The OAuth configuration.</param>
    /// <param name="tokens">The token provider.</param>
    /// <returns>A client that authorizes every send.</returns>
    public static HttpClient CreateClient(HttpClient client, OAuthAuthorization authorization, IOAuthClient tokens) =>
        TokenHttpMessageHandler.CreateClient(
            client,
            (eventStore, cancellation) => tokens.AcquireToken(authorization, eventStore, cancellation),
            token => tokens.Invalidate(authorization, token));

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        _handler.SendAsync(request, cancellationToken);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _handler.Dispose();
        base.Dispose(disposing);
    }
}
