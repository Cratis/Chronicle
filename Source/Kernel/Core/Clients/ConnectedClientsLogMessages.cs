// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Holds log messages for <see cref="ConnectedClients"/>.
/// </summary>
internal static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(LogLevel.Information, "ConnectedClients activated on silo {Silo}")]
    internal static partial void Activated(this ILogger<ConnectedClients> logger, string silo);

    [LoggerMessage(LogLevel.Information, "Registry on {Silo} keyed for {RegistryKey} read live connections from store {StoreId}: keyed for this silo {KeyedForThisSilo}, {Count} clients registered after reseed")]
    internal static partial void LocalConnectionStoreRead(this ILogger<ConnectedClients> logger, string silo, string registryKey, Guid storeId, bool keyedForThisSilo, int count);

    [LoggerMessage(LogLevel.Information, "Registry on {Silo} seeded {Count} live connections open on this silo after activation")]
    internal static partial void SeededLiveConnections(this ILogger<ConnectedClients> logger, string silo, int count);

    [LoggerMessage(LogLevel.Information, "ConnectedClients on silo {Silo}: client {ConnectionId} connected, {Count} clients registered")]
    internal static partial void ClientConnectedOnSilo(this ILogger<ConnectedClients> logger, string silo, string connectionId, int count);

    [LoggerMessage(LogLevel.Information, "ConnectedClients on silo {Silo}: client {ConnectionId} disconnected ({Reason}), {Count} clients registered")]
    internal static partial void ClientDisconnectedOnSilo(this ILogger<ConnectedClients> logger, string silo, string connectionId, string reason, int count);

    [LoggerMessage(LogLevel.Debug, "Connected client with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger, string connectionId);

    [LoggerMessage(LogLevel.Information, "Restored client with connection identifier '{ConnectionId}', which the registry had lost while it stayed connected.")]
    internal static partial void ClientRestored(this ILogger<ConnectedClients> logger, string connectionId);

    [LoggerMessage(LogLevel.Debug, "Disconnected client with connection identifier '{ConnectionId}'. Reason: {Reason}")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger, string connectionId, string reason);
}
