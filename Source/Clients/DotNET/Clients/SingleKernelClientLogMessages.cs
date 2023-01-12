// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

public static partial class SingleKernelClientLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Connecting to Cratis Kernel @ '{Endpoint}'")]
    public static partial void Connecting(this ILogger<SingleKernelClient> logger, string endpoint);

    [LoggerMessage(1, LogLevel.Trace, "Kernel unavailable. Retrying.")]
    public static partial void KernelUnavailable(this ILogger<SingleKernelClient> logger);

    [LoggerMessage(2, LogLevel.Trace, "Performing command '{Route}' on Kernel @ '{Endpoint}'")]
    public static partial void PerformingCommand(this ILogger<SingleKernelClient> logger, string endpoint, string route);

    [LoggerMessage(3, LogLevel.Trace, "Performing query '{Route}' on Kernel @ '{Endpoint}'")]
    public static partial void PerformingQuery(this ILogger<SingleKernelClient> logger, string endpoint, string route);
}
