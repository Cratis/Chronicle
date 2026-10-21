// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Structured target-level delivery logging.
/// </summary>
internal static partial class WebhookMediatorLogging
{
    /// <summary>
    /// Records the deadline of an unavailable target.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The normalized target authority.</param>
    /// <param name="deadline">The earliest next delivery.</param>
    [LoggerMessage(LogLevel.Information, "Webhook target {Authority} unavailable: circuit open until {Deadline}")]
    internal static partial void TargetUnavailable(this ILogger<WebhookMediator> logger, string authority, DateTimeOffset deadline);

    /// <summary>
    /// Records receiver back-pressure without consuming a delivery attempt.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The normalized target authority.</param>
    /// <param name="statusCode">The response status.</param>
    /// <param name="deadline">The earliest next delivery.</param>
    [LoggerMessage(LogLevel.Information, "Webhook target {Authority} returned {StatusCode}: deferring until {Deadline}")]
    internal static partial void TargetRateLimited(this ILogger<WebhookMediator> logger, string authority, int statusCode, DateTimeOffset deadline);

    /// <summary>
    /// Records a permanent rejection, requiring operator intervention rather than more retries.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The normalized target authority.</param>
    [LoggerMessage(LogLevel.Error, "Webhook target {Authority} returned 410 Gone: parking partition immediately; no automatic retries")]
    internal static partial void TargetGone(this ILogger<WebhookMediator> logger, string authority);

    /// <summary>
    /// Records a configuration error that pauses observer delivery.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The normalized target authority.</param>
    /// <param name="statusCode">The response status.</param>
    [LoggerMessage(LogLevel.Error, "Webhook target {Authority} returned {StatusCode}: pausing observer; correct configuration and clear observer quarantine to resume")]
    internal static partial void TargetConfigurationRejected(this ILogger<WebhookMediator> logger, string authority, int statusCode);
}
