// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages for <see cref="FederationServeService"/>. Never logs certificate material or event content.
/// </summary>
internal static partial class FederationServeServiceLogging
{
    [LoggerMessage(LogLevel.Debug, "Federation serve refused: {Reason}")]
    internal static partial void ServeRefused(this ILogger<FederationServeService> logger, string reason);

    [LoggerMessage(LogLevel.Information, "Federation serve session of subject '{Subject}' started, resuming from {Cursor}")]
    internal static partial void ServeStarted(this ILogger<FederationServeService> logger, string subject, ulong cursor);

    [LoggerMessage(LogLevel.Information, "Federation serve session of subject '{Subject}' ended: {Reason}")]
    internal static partial void ServeEnded(this ILogger<FederationServeService> logger, string subject, string reason);

    [LoggerMessage(LogLevel.Warning, "Federation serve session of subject '{Subject}' rejected an event claiming this store as its origin")]
    internal static partial void ServeOwnOriginRejected(this ILogger<FederationServeService> logger, string subject);

    [LoggerMessage(LogLevel.Warning, "Federation serve session of subject '{Subject}' failed to append to the inbox: {Failure}")]
    internal static partial void ServeAppendFailed(this ILogger<FederationServeService> logger, string subject, string failure);
}
