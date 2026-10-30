// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ConnectionServiceLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failure during keep alive for connection {ConnectionId}")]
    internal static partial void FailureDuringKeepAlive(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "A {ClientType} client at version {ClientVersion} speaking protocol {ProtocolVersion} expects {IncompatibilityCount} contract element(s) this server no longer serves")]
    internal static partial void ClientIsIncompatible(this ILogger<ConnectionService> logger, string clientType, string clientVersion, string protocolVersion, int incompatibilityCount);

    [LoggerMessage(LogLevel.Warning, "Could not read the descriptor set sent by a {ClientType} client")]
    internal static partial void FailedToReadClientDescriptorSet(this ILogger<ConnectionService> logger, string clientType, Exception exception);

    [LoggerMessage(LogLevel.Information, "The registry of connected clients no longer knows connection {ConnectionId}, which is still open. Registering it again")]
    internal static partial void RegisteringClientAgain(this ILogger<ConnectionService> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "Could not check that connection {ConnectionId} is still registered. Retrying at the next keep-alive")]
    internal static partial void FailedToKeepClientRegistered(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Information, "Connection {ConnectionId} on {Silo} opened in store {StoreId}")]
    internal static partial void ConnectionOpened(this ILogger<ConnectionService> logger, string connectionId, string silo, Guid storeId);

    [LoggerMessage(LogLevel.Information, "Connection {ConnectionId} on {Silo} closed in store {StoreId}")]
    internal static partial void ConnectionClosed(this ILogger<ConnectionService> logger, string connectionId, string silo, Guid storeId);

    [LoggerMessage(LogLevel.Information, "Keep-alive from {ConnectionId} on {Silo}: registry knows client {RegistryKnowsClient}, open {Open}, lapsed {Lapsed}, gap {GapMs} ms, restoring {Restoring}, store {StoreId}")]
    internal static partial void KeepAliveReceived(this ILogger<ConnectionService> logger, string connectionId, string silo, bool registryKnowsClient, bool open, bool lapsed, long gapMs, bool restoring, Guid storeId);

    [LoggerMessage(LogLevel.Information, "Keep-alive from {ConnectionId} on {Silo} failed with {ExceptionType}")]
    internal static partial void KeepAliveFailed(this ILogger<ConnectionService> logger, string connectionId, string silo, string exceptionType, Exception exception);

    [LoggerMessage(LogLevel.Information, "Connect loop for {ConnectionId} on {Silo} ended: {Reason}")]
    internal static partial void ConnectLoopEnded(this ILogger<ConnectionService> logger, string connectionId, string silo, string reason);
}
