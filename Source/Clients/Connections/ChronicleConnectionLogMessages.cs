// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ChronicleConnectionLogMessages
{
    /// <summary>
    /// Logs that a connection is being established.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> to log to.</param>
    /// <param name="redactedConnectionString">The connection string with its credentials masked - <see cref="ChronicleConnectionString.Redacted"/>, never <see cref="ChronicleConnectionString.ToString"/>.</param>
    [LoggerMessage(LogLevel.Information, "Connecting to Chronicle ({RedactedConnectionString})")]
    internal static partial void Connecting(this ILogger<ChronicleConnection> logger, string redactedConnectionString);

    [LoggerMessage(LogLevel.Information, "Configuring Chronicle connection with connection string: {RedactedConnectionString}")]
    internal static partial void Configuring(this ILogger<ChronicleConnection> logger, string redactedConnectionString);

    [LoggerMessage(LogLevel.Information, "Connected to Chronicle")]
    internal static partial void Connected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Information, "Disconnected from Chronicle")]
    internal static partial void Disconnected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Error, "Timed out during connecting to Chronicle")]
    internal static partial void TimedOut(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Warning, "Could not complete the handshake with Chronicle server {Server}: {Reason}")]
    internal static partial void EndpointHandshakeFailed(this ILogger<ChronicleConnection> logger, string server, string reason);

    [LoggerMessage(LogLevel.Warning, "The keep-alive stream from Chronicle server {Server} ended, reconnecting")]
    internal static partial void KeepAliveStreamEnded(this ILogger<ChronicleConnection> logger, string server);

    [LoggerMessage(LogLevel.Warning, "Chronicle at {Server} did not accept the connection within {TimeoutSeconds} seconds - it may still be starting; retrying in the background")]
    internal static partial void NotAcceptingConnectionsYet(this ILogger<ChronicleConnection> logger, string server, int timeoutSeconds);

    [LoggerMessage(LogLevel.Warning, "Server certificate validation is disabled for {Servers}; any certificate is accepted. Set skipTlsValidation=false once the server certificate is trusted.")]
    internal static partial void ServerCertificateValidationDisabled(this ILogger<ChronicleConnection> logger, string servers);

    [LoggerMessage(LogLevel.Debug, "Using client certificate from {Path}")]
    internal static partial void UsingClientCertificate(this ILogger<ChronicleConnection> logger, string path);

    [LoggerMessage(LogLevel.Debug, "Grpc channel created for address {Address}")]
    internal static partial void ChannelCreated(this ILogger<ChronicleConnection> logger, string address);
}
