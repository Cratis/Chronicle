// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Holds log messages for <see cref="ClientConnection"/>.
/// </summary>
internal static partial class ClientConnectionLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Client connection '{ConnectionId}' disconnected, notifying {SubscriberCount} subscriber(s).")]
    internal static partial void Disconnected(this ILogger<ClientConnection> logger, string connectionId, int subscriberCount);

    [LoggerMessage(LogLevel.Warning, "Failed notifying subscriber '{Subscriber}' that client connection '{ConnectionId}' disconnected.")]
    internal static partial void FailedNotifyingSubscriber(this ILogger<ClientConnection> logger, Exception exception, string connectionId, string subscriber);
}
