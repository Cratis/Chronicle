// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class SingleKernelClientLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Connecting to Cratis Kernel @ '{Endpoint}'")]
    internal static partial void Connecting(this ILogger<SingleKernelClient> logger, string endpoint);

    [LoggerMessage(1, LogLevel.Information, "Cratis Kernel unavailable. Retrying.")]
    internal static partial void KernelUnavailable(this ILogger<SingleKernelClient> logger);

    [LoggerMessage(2, LogLevel.Trace, "Performing command '{Route}' on Kernel @ '{Endpoint}'")]
    internal static partial void PerformingCommand(this ILogger<SingleKernelClient> logger, string endpoint, string route);

    [LoggerMessage(3, LogLevel.Trace, "Performing query '{Route}' on Kernel @ '{Endpoint}'")]
    internal static partial void PerformingQuery(this ILogger<SingleKernelClient> logger, string endpoint, string route);

    [LoggerMessage(4, LogLevel.Trace, "Result of performing command '{Route}' - Success: '{Success}'")]
    internal static partial void CommandResult(this ILogger<SingleKernelClient> logger, string route, bool success);

    [LoggerMessage(5, LogLevel.Trace, "Result of performing command '{Route}' - Exceptions: '{Exceptions}'")]
    internal static partial void CommandResultExceptions(this ILogger<SingleKernelClient> logger, string route, IEnumerable<string> exceptions);

    [LoggerMessage(6, LogLevel.Trace, "Result of performing command '{Route}' - Validation failed for members '{Members}' with message '{Message}'")]
    internal static partial void CommandResultValidationError(this ILogger<SingleKernelClient> logger, string route, string members, string message);

    [LoggerMessage(7, LogLevel.Trace, "Result of performing query '{Route}' - Success: '{Success}'")]
    internal static partial void QueryResult(this ILogger<SingleKernelClient> logger, string route, bool success);

    [LoggerMessage(8, LogLevel.Trace, "Result of performing query '{Route}' - Exceptions: '{Exceptions}'")]
    internal static partial void QueryResultExceptions(this ILogger<SingleKernelClient> logger, string route, IEnumerable<string> exceptions);

    [LoggerMessage(9, LogLevel.Trace, "Result of performing query '{Route}' - Validation failed for members '{Members}' with message '{Message}'")]
    internal static partial void QueryResultValidationError(this ILogger<SingleKernelClient> logger, string route, string members, string message);

    [LoggerMessage(10, LogLevel.Information, "Kernel disconnected. Retrying to connect.")]
    internal static partial void KernelDisconnected(this ILogger<SingleKernelClient> logger);

    [LoggerMessage(11, LogLevel.Information, "Connected to Cratis Kernel")]
    internal static partial void KernelConnected(this ILogger<SingleKernelClient> logger);
}
