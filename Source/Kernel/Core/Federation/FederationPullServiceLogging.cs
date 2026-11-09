// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages for <see cref="FederationPullService"/>. Never logs certificate material or event content.
/// </summary>
internal static partial class FederationPullServiceLogging
{
    [LoggerMessage(LogLevel.Debug, "Federation pull denied: {Reason}")]
    internal static partial void PullDenied(this ILogger<FederationPullService> logger, string reason);

    [LoggerMessage(LogLevel.Debug, "Federation pull throttled for link {LinkId}")]
    internal static partial void PullThrottled(this ILogger<FederationPullService> logger, string linkId);

    [LoggerMessage(LogLevel.Warning, "Federation did not forward {Count} event(s) of event type {EventTypeId} on link {LinkId} because the event type carries protected personal data")]
    internal static partial void SkippedProtected(this ILogger<FederationPullService> logger, int count, string eventTypeId, string linkId);
}
