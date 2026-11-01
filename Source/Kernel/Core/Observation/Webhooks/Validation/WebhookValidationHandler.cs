// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Checks every request, including requests using an existing pooled connection.
/// </summary>
/// <param name="policy">The target policy.</param>
/// <param name="logger">The logger.</param>
/// <param name="evaluateTarget">Whether to evaluate and log the complete target policy before sending.</param>
public class WebhookValidationHandler(IWebhookTargetPolicy policy, ILogger<WebhookValidationHandler> logger, bool evaluateTarget = false) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        EventStoreName? eventStore = null;
        if (request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var store))
        {
            eventStore = store;
        }
        else
        {
            logger.UsingGlobalPolicy();
        }

        var result = evaluateTarget
            ? await policy.Evaluate(request.RequestUri!.AbsoluteUri, eventStore, cancellationToken)
            : policy.CheckAuthority(request.RequestUri!, eventStore);
        if (!result.IsAllowed) throw new WebhookTargetRejected(result.Reason);

        // An approved host can resolve to a store-specific private destination. Set this before the
        // primary handler selects a connection; doing it inside ConnectCallback is too late for pooling.
        var dedicated = policy.RequiresDedicatedConnection(request.RequestUri!, eventStore);
        request.Options.Set(WebhookRequestOptions.DedicatedConnection, dedicated);
        if (dedicated)
        {
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            request.Headers.ConnectionClose = true;
        }

        var response = await base.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new WebhookTargetRejected($"Webhook redirects are not allowed (HTTP {status}).");
        }

        return response;
    }
}
