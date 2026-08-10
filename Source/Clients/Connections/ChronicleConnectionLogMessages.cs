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

    [LoggerMessage(LogLevel.Information, "Connected to Chronicle")]
    internal static partial void Connected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Information, "Disconnected from Chronicle")]
    internal static partial void Disconnected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Error, "Timed out during connecting to Chronicle")]
    internal static partial void TimedOut(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Debug, "Using client certificate from {Path}")]
    internal static partial void UsingClientCertificate(this ILogger<ChronicleConnection> logger, string path);

    [LoggerMessage(LogLevel.Debug, "Grpc channel created for address {Address}")]
    internal static partial void ChannelCreated(this ILogger<ChronicleConnection> logger, string address);
}
