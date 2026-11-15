// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a webhook target rejects a delivery with a non-success HTTP status.
/// </summary>
public class WebhookDeliveryFailed : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDeliveryFailed"/> class.
    /// </summary>
    /// <param name="targetUrl">The URL of the webhook target, projected to a safe diagnostic authority.</param>
    /// <param name="statusCode">The HTTP status code returned by the target.</param>
    /// <param name="reasonPhrase">The HTTP reason phrase, intentionally not retained.</param>
    public WebhookDeliveryFailed(WebhookTargetUrl targetUrl, HttpStatusCode statusCode, string? reasonPhrase)
        : base($"Webhook delivery to '{WebhookDiagnostics.Authority(targetUrl)}' failed with HTTP {(int)statusCode} ({statusCode}).")
    {
        _ = reasonPhrase;
        TargetUrl = WebhookDiagnostics.Authority(targetUrl);
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the safe diagnostic authority of the webhook target, or an opaque fallback.
    /// </summary>
    public WebhookTargetUrl TargetUrl { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the target.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the HTTP reason phrase, always null to avoid retaining receiver-controlled content.
    /// </summary>
    public string? ReasonPhrase { get; }
}
