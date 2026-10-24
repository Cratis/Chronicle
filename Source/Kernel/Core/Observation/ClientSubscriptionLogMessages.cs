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
    [LoggerMessage(LogLevel.Information, "Observer {ObserverId} does not know client {ConnectionId}, which is still connected. Restoring its subscription")]
    internal static partial void RestoringLostClientSubscription(this ILogger logger, ObserverId observerId, ConnectionId connectionId);
}
