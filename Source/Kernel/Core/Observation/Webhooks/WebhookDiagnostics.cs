// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Projects untrusted delivery information for diagnostic sinks, never routing.
/// </summary>
internal static class WebhookDiagnostics
{
    /// <summary>
    /// The opaque fallback for an unprojectable target.
    /// </summary>
    internal const string Redacted = "REDACTED";

    /// <summary>
    /// Projects a target for display without changing the delivery target.
    /// </summary>
    /// <param name="url">The raw target.</param>
    /// <returns>The display authority.</returns>
    internal static string Authority(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        !string.IsNullOrEmpty(uri.Host)
            ? uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)
            : Redacted;

    /// <summary>
    /// Describes a delivery failure for a diagnostic sink.
    /// </summary>
    /// <param name="target">The delivery target.</param>
    /// <param name="error">The failure.</param>
    /// <returns>The diagnostic text.</returns>
    internal static string Describe(WebhookTarget target, Exception error) => error switch
    {
        WebhookDeliveryFailed failure => $"Webhook delivery to '{Authority(target.Url)}' failed with HTTP {(int)failure.StatusCode} ({failure.StatusCode}).",
        WebhookTargetRejected or ManagedIdentityWebhookNotAllowed or ManagedIdentityWebhookDeliveryFailed => error.Message,
        TaskCanceledException => "Webhook delivery timed out.",
        HttpRequestException request => $"Webhook delivery to '{Authority(target.Url)}' failed ({TransportClassification(request)}).",
        _ => $"Webhook delivery to '{Authority(target.Url)}' failed ({error.GetType().Name})."
    };

    static string TransportClassification(HttpRequestException error) =>
        error.GetBaseException() is SocketException socket
            ? $"{error.HttpRequestError}/{socket.SocketErrorCode}"
            : error.HttpRequestError.ToString();
}
