// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class ChronicleConnectionLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void Connecting(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(2, LogLevel.Information, "Connected to Cratis Kernel")]
    internal static partial void Connected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(3, LogLevel.Information, "Disconnected from Cratis Kernel")]
    internal static partial void Disconnected(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(4, LogLevel.Information, "Reconnecting to Cratis Kernel")]
    internal static partial void Reconnecting(this ILogger<ChronicleConnection> logger);

    [LoggerMessage(5, LogLevel.Error, "Timed out during connecting to Cratis Kernel")]
    internal static partial void TimedOut(this ILogger<ChronicleConnection> logger);
}
