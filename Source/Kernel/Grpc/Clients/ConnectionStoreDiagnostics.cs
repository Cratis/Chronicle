// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Diagnostic-only logging for registry restoration on l189/5603-diag. Never logs connection payloads or caller identity.
/// </summary>
internal static partial class ConnectionStoreDiagnostics
{
    [LoggerMessage(LogLevel.Debug, "DIAG connection store {Phase}: connection {ConnectionId}, store {StoreId}, silo {Silo}, IsOpen {IsOpen}")]
    internal static partial void DiagnosticConnectionStore(this ILogger<ConnectionService> logger, string phase, string connectionId, int storeId, string silo, bool isOpen);

    [LoggerMessage(LogLevel.Debug, "DIAG registry ping: connection {ConnectionId}, registryKnowsClient {RegistryKnowsClient}, respondedAt {RespondedAt}, lapse threshold {Lapse}")]
    internal static partial void DiagnosticRegistryPing(this ILogger<ConnectionService> logger, string connectionId, bool registryKnowsClient, DateTimeOffset respondedAt, TimeSpan lapse);

    [LoggerMessage(LogLevel.Debug, "DIAG lapse decision {Decision}: connection {ConnectionId}, HasLapsed {HasLapsed}")]
    internal static partial void DiagnosticLapseDecision(this ILogger<ConnectionService> logger, string decision, string connectionId, bool hasLapsed);
}
