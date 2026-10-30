// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Administrator authorization diagnostics.
/// </summary>
internal static partial class AdministratorAuthorizationLogging
{
    /// <summary>
    /// Records an unavailable authority check without logging caller credentials.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(LogLevel.Error, "Administrator authority could not be established; denying the call")]
    internal static partial void AdministratorCheckFailed(this ILogger logger, Exception exception);

    /// <summary>
    /// Records an operational denial that is not enforced in audit mode.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="callerKind">The caller kind, never token content.</param>
    /// <param name="command">The command name.</param>
    [LoggerMessage(LogLevel.Warning, "Administrator policy would deny {CallerKind} calling {Command}; audit mode allows the call")]
    internal static partial void AdministratorOperationWouldDeny(this ILogger logger, string callerKind, string command);
}
