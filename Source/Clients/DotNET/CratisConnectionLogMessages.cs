// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

internal static partial class CratisConnectionLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Connecting to Cratis Kernel")]
    internal static partial void Connecting(this ILogger<CratisConnection> logger);

    [LoggerMessage(2, LogLevel.Information, "Connected to Cratis Kernel")]
    internal static partial void Connected(this ILogger<CratisConnection> logger);

    [LoggerMessage(3, LogLevel.Information, "Disconnected from Cratis Kernel")]
    internal static partial void Disconnected(this ILogger<CratisConnection> logger);

    [LoggerMessage(4, LogLevel.Information, "Reconnecting to Cratis Kernel")]
    internal static partial void Reconnecting(this ILogger<CratisConnection> logger);

    [LoggerMessage(5, LogLevel.Information, "Timed out during connecting to Cratis Kernel")]
    internal static partial void TimedOut(this ILogger<CratisConnection> logger);
}
