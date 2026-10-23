// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Log messages for <see cref="ClientSubscriptionKeeper"/>.
/// </summary>
internal static partial class ClientSubscriptionKeeperLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Keeping the subscription of client {ConnectionId} failed. Retrying at the next check")]
    internal static partial void KeepingClientSubscriptionFailed(this ILogger logger, ConnectionId connectionId, Exception exception);
}
