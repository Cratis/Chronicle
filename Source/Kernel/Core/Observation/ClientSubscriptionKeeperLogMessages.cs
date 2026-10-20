// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Log messages for <see cref="ClientSubscriptionKeeper"/>.
/// </summary>
internal static partial class ClientSubscriptionKeeperLogMessages
{
    [LoggerMessage(LogLevel.Information, "The observer no longer knows client {ConnectionId}, which is still connected. Restoring its subscription")]
    internal static partial void RestoringLostClientSubscription(this ILogger logger, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Warning, "Restoring the subscription of client {ConnectionId} failed. Retrying at the next check")]
    internal static partial void RestoringLostClientSubscriptionFailed(this ILogger logger, ConnectionId connectionId, Exception exception);
}
