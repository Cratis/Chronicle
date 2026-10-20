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
}
