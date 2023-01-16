// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

public static partial class SingleKernelClientLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Connecting to Cratis Kernel @ '{Endpoint}'")]
    internal static partial void Connecting(this ILogger<SingleKernelClient> logger, string endpoint);

    [LoggerMessage(1, LogLevel.Trace, "Kernel unavailable. Retrying.")]
    internal static partial void KernelUnavailable(this ILogger<SingleKernelClient> logger);

    [LoggerMessage(2, LogLevel.Trace, "Performing command '{Route}' on Kernel @ '{Endpoint}'")]
    internal static partial void PerformingCommand(this ILogger<SingleKernelClient> logger, string endpoint, string route);

    [LoggerMessage(3, LogLevel.Trace, "Performing query '{Route}' on Kernel @ '{Endpoint}'")]
    internal static partial void PerformingQuery(this ILogger<SingleKernelClient> logger, string endpoint, string route);

    [LoggerMessage(4, LogLevel.Trace, "Result of performing command '{Route}' - Success: '{Success}'")]
    internal static  partial void CommandResult(this ILogger<SingleKernelClient> logger, string route, bool success);

    [LoggerMessage(4, LogLevel.Trace, "Result of performing command '{Route}' - Exceptions: '{Exceptions}'")]
    internal static  partial void CommandResultExceptions(this ILogger<SingleKernelClient> logger, string route, IEnumerable<string> exceptions);

    [LoggerMessage(5, LogLevel.Trace, "Result of performing command '{Route}' - Validation failed for members '{Members}' with message '{Message}'")]
    internal static  partial void CommandResultValidationError(this ILogger<SingleKernelClient> logger, string route, string members, string message);
}
