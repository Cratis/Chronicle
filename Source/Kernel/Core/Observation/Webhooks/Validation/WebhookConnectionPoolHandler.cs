// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Separates ordinary pools from requests using explicit host allowances, which must never be reused.
/// Both primary handlers are bounded by the owning HTTP client factory handler lifetime.
/// </summary>
public class WebhookConnectionPoolHandler : DelegatingHandler
{
    readonly HttpMessageInvoker _dedicated;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookConnectionPoolHandler"/> class.
    /// </summary>
    /// <param name="dedicated">The guarded primary handler for requests using host allowances.</param>
    public WebhookConnectionPoolHandler(SocketsHttpHandler dedicated)
    {
        dedicated.PooledConnectionLifetime = TimeSpan.Zero;
        _dedicated = new(dedicated);
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.Options.TryGetValue(WebhookRequestOptions.DedicatedConnection, out var dedicated) && dedicated
            ? _dedicated.SendAsync(request, cancellationToken)
            : base.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) _dedicated.Dispose();
        base.Dispose(disposing);
    }
}
