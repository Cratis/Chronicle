// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ChronicleConnectionLogMessages
{
    [LoggerMessage(LogLevel.Information, "Connecting to Chronicle ({ConnectionString})")]
    internal static partial void Connecting(this ILogger<ChronicleConnection> logger, ChronicleConnectionString connectionString);

    [LoggerMessage(LogLevel.Information, "Connected to Chronicle")]
    internal static partial void Connected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Information, "Disconnected from Chronicle")]
    internal static partial void Disconnected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Debug, "Reconnecting to Chronicle")]
    internal static partial void Reconnecting(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Error, "Timed out during connecting to Chronicle")]
    internal static partial void TimedOut(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(LogLevel.Debug, "Using client certificate from {Path}")]
    internal static partial void UsingClientCertificate(this ILogger<ChronicleConnection> logger, string path);

    [LoggerMessage(LogLevel.Debug, "Grpc channel created for address {Address}")]
    internal static partial void ChannelCreated(this ILogger<ChronicleConnection> logger, string address);
}
