// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

internal static partial class ReactorInvokerLogMessages
{
    [LoggerMessage(LogLevel.Error, "Reactor of type '{ReactorId}' failed for event with type '{EventType}'")]
    internal static partial void ReactorFailed(this ILogger<ReactorInvoker> logger, ReactorId ReactorId, string eventType, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Reactor of type '{ReactorId}' stopped handling event with type '{EventType}' because its call to the kernel was cancelled or disposed")]
    internal static partial void ReactorCancelledByKernelConnection(this ILogger<ReactorInvoker> logger, ReactorId ReactorId, string eventType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Reactor of type '{ReactorId}' could not finish handling event with type '{EventType}' because the kernel is stopping or unreachable")]
    internal static partial void ReactorInterruptedByLostKernelConnection(this ILogger<ReactorInvoker> logger, ReactorId ReactorId, string eventType, Exception exception);
}
