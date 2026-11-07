// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Log messages for <see cref="StoreCapabilityDecision"/>.
/// </summary>
internal static partial class StoreCapabilityDecisionLogMessages
{
    /// <summary>
    /// Records a decision that did not grant the capability, with the outcome the interim rule gave it.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="reason">The reason of the decision.</param>
    /// <param name="permitted">Whether the query is allowed.</param>
    internal static void Decided(this ILogger logger, string capability, string reason, bool permitted)
    {
        if (permitted) logger.Tolerated(capability, reason);
        else logger.Refused(capability, reason);
    }

    [LoggerMessage(LogLevel.Debug, "Capability {Capability} is not granted to the caller (reason {Reason}); the query is allowed for this event store")]
    static partial void Tolerated(this ILogger logger, string capability, string reason);

    [LoggerMessage(LogLevel.Warning, "Capability {Capability} is not granted to the caller (reason {Reason}); the query is refused")]
    static partial void Refused(this ILogger logger, string capability, string reason);
}
