// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Connections;

internal static partial class RestKernelConnectionLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Connecting to Cratis Kernel")]
    internal static partial void Connecting(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(1, LogLevel.Information, "Cratis Kernel unavailable. Retrying.")]
    internal static partial void KernelUnavailable(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(2, LogLevel.Trace, "Performing command '{Route}' on Kernel")]
    internal static partial void PerformingCommand(this ILogger<RestKernelConnection> logger, string route);

    [LoggerMessage(3, LogLevel.Trace, "Performing query '{Route}' on Kernel")]
    internal static partial void PerformingQuery(this ILogger<RestKernelConnection> logger, string route);

    [LoggerMessage(4, LogLevel.Trace, "Result of performing command '{Route}' - Success: '{Success}'")]
    internal static partial void CommandResult(this ILogger<RestKernelConnection> logger, string route, bool success);

    [LoggerMessage(5, LogLevel.Trace, "Result of performing command '{Route}' - Exceptions: '{Exceptions}' - StackTrace: '{ExceptionStackTrace}'")]
    internal static partial void CommandResultExceptions(this ILogger<RestKernelConnection> logger, string route, IEnumerable<string> exceptions, string exceptionStackTrace);

    [LoggerMessage(6, LogLevel.Trace, "Result of performing command '{Route}' - Validation failed for members '{Members}' with message '{Message}'")]
    internal static partial void CommandResultValidationResult(this ILogger<RestKernelConnection> logger, string route, string members, string message);

    [LoggerMessage(7, LogLevel.Trace, "Result of performing query '{Route}' - Success: '{Success}'")]
    internal static partial void QueryResult(this ILogger<RestKernelConnection> logger, string route, bool success);

    [LoggerMessage(8, LogLevel.Trace, "Result of performing query '{Route}' - Exceptions: '{Exceptions}' - StackTrace: '{ExceptionStackTrace}'")]
    internal static partial void QueryResultExceptions(this ILogger<RestKernelConnection> logger, string route, IEnumerable<string> exceptions, string exceptionStackTrace);

    [LoggerMessage(9, LogLevel.Trace, "Result of performing query '{Route}' - Validation failed for members '{Members}' with message '{Message}'")]
    internal static partial void QueryResultValidationResult(this ILogger<RestKernelConnection> logger, string route, string members, string message);

    [LoggerMessage(10, LogLevel.Information, "Kernel disconnected. Retrying to connect.")]
    internal static partial void KernelDisconnected(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(11, LogLevel.Information, "Connected to Cratis Kernel")]
    internal static partial void KernelConnected(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(12, LogLevel.Information, "Setting up client ping")]
    internal static partial void SettingUpClientPing(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(13, LogLevel.Information, "Client ping is not enabled, since the debugger is attached")]
    internal static partial void NoPing(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(14, LogLevel.Information, "Attempting to connect")]
    internal static partial void AttemptingConnect(this ILogger<RestKernelConnection> logger);

    [LoggerMessage(15, LogLevel.Error, "Error during connection")]
    internal static partial void Error(this ILogger<RestKernelConnection> logger, Exception exception);
}
