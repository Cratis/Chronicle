// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Authorization decision diagnostics.
/// </summary>
internal static partial class AuthorizationDecisionsLogging
{
    /// <summary>
    /// Records a decision that could not be established, without logging caller credentials.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="capability">The capability name.</param>
    [LoggerMessage(LogLevel.Error, "Authorization decision for capability {Capability} could not be established; denying")]
    internal static partial void AuthorizationDecisionFailed(this ILogger logger, Exception exception, string capability);
}
