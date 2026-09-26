// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a webhook target rejects a delivery with a non-success HTTP status.
/// </summary>
/// <param name="targetUrl">The URL of the webhook target.</param>
/// <param name="statusCode">The HTTP status code returned by the target.</param>
/// <param name="reasonPhrase">The HTTP reason phrase returned by the target.</param>
public class WebhookDeliveryFailed(WebhookTargetUrl targetUrl, HttpStatusCode statusCode, string? reasonPhrase)
    : Exception($"Webhook delivery to '{targetUrl}' failed with HTTP {(int)statusCode} ({statusCode}): {reasonPhrase}")
{
    /// <summary>
    /// Gets the URL of the webhook target.
    /// </summary>
    public WebhookTargetUrl TargetUrl { get; } = targetUrl;

    /// <summary>
    /// Gets the HTTP status code returned by the target.
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Gets the HTTP reason phrase returned by the target.
    /// </summary>
    public string? ReasonPhrase { get; } = reasonPhrase;
}
