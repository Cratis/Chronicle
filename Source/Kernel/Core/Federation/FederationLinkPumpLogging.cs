// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages of the federation consumer. Never carries certificate material, pins or event content.
/// </summary>
internal static partial class FederationLinkPumpLogging
{
    [LoggerMessage(LogLevel.Information, "Federation binding '{BindingId}' is removed or federation is disabled; the consumer stops")]
    internal static partial void LinkDisabled(this ILogger<FederationLinkPump> logger, string bindingId);

    [LoggerMessage(LogLevel.Warning, "Federation binding '{BindingId}' was denied by the producer; retrying rarely")]
    internal static partial void LinkDenied(this ILogger<FederationLinkPump> logger, string bindingId);

    [LoggerMessage(LogLevel.Warning, "Federation binding '{BindingId}' rejected an event that originated in this event store; loop prevented")]
    internal static partial void OwnOriginRejected(this ILogger<FederationLinkPump> logger, string bindingId);

    [LoggerMessage(LogLevel.Warning, "Federation binding '{BindingId}' failed during {Kind}: {Error}")]
    internal static partial void LinkFailed(this ILogger<FederationLinkPump> logger, string bindingId, string kind, string error);

    [LoggerMessage(LogLevel.Information, "Federation binding '{BindingId}' resumes from source sequence number {Cursor}")]
    internal static partial void Resumed(this ILogger<FederationLinkPump> logger, string bindingId, ulong cursor);
}
