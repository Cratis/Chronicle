// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Log messages for keeping the subscription of a connected client.
/// </summary>
internal static partial class ClientSubscriptionLogMessages
{
    // Written on every keep-alive the observer turns down too - a client still subscribed in a namespace that was
    // removed is never restored - so it is Debug. The observer logs a restore it actually performs.
    [LoggerMessage(LogLevel.Debug, "Observer {ObserverId} does not know client {ConnectionId}, which is still connected. Asking it to restore the subscription")]
    internal static partial void RestoringLostClientSubscription(this ILogger logger, ObserverId observerId, ConnectionId connectionId);
}
