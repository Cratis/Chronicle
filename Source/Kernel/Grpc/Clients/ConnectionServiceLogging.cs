// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ConnectionServiceLogMessages
{
    [LoggerMessage(LogLevel.Error, "Could not register client connection {ConnectionId}")]
    internal static partial void ClientRegistrationFailed(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failure while running or closing connection {ConnectionId}")]
    internal static partial void ConnectionFailed(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failure during keep alive for connection {ConnectionId}")]
    internal static partial void FailureDuringKeepAlive(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "A {ClientType} client at version {ClientVersion} speaking protocol {ProtocolVersion} expects {IncompatibilityCount} contract element(s) this server no longer serves")]
    internal static partial void ClientIsIncompatible(this ILogger<ConnectionService> logger, string clientType, string clientVersion, string protocolVersion, int incompatibilityCount);

    [LoggerMessage(LogLevel.Warning, "Could not read the descriptor set sent by a {ClientType} client")]
    internal static partial void FailedToReadClientDescriptorSet(this ILogger<ConnectionService> logger, string clientType, Exception exception);

    [LoggerMessage(LogLevel.Information, "The registry of connected clients no longer knows connection {ConnectionId}, which is still open. Registering it again")]
    internal static partial void RegisteringClientAgain(this ILogger<ConnectionService> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "Connection {ConnectionId} was not opened because the caller could not be identified")]
    internal static partial void ConnectionRefusedWithoutIdentity(this ILogger<ConnectionService> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "Connection {ConnectionId} was not opened because it is not available to the caller")]
    internal static partial void ConnectionRefusedForAnotherCaller(this ILogger<ConnectionService> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "A keep-alive for connection {ConnectionId} was refused because the connection is not available to the caller")]
    internal static partial void KeepAliveRefused(this ILogger<ConnectionService> logger, string connectionId);

    [LoggerMessage(LogLevel.Warning, "Could not check that connection {ConnectionId} is still registered. Retrying at the next keep-alive")]
    internal static partial void FailedToKeepClientRegistered(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Connection {ConnectionId} was not opened because its reservation could not be verified across the cluster")]
    internal static partial void ConnectionReservationFailed(this ILogger<ConnectionService> logger, string connectionId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The cluster-wide reservation for connection {ConnectionId} could not be released")]
    internal static partial void FailedToReleaseReservation(this ILogger<ConnectionService> logger, string connectionId, Exception exception);
}
