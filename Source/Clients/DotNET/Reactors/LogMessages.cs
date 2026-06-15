// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents log messages.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to activate reactor middleware of type {MiddlewareType}.")]
    internal static partial void FailedToActivateReactorMiddleware(this ILogger<ReactorMiddlewaresActivator> logger, Type middlewareType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to perform after invocation action on reactor middlewares while reacting to {EventTypeName} in reactor {ReactorId}.")]
    internal static partial void ReactorMiddlewareAfterInvokeFailed(this ILogger<ReactorInvoker> logger, ReactorId reactorId, string eventTypeName, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Could not find a reactor handler method in reactor {ReactorId} for {EventTypeName}.")]
    internal static partial void ReactorNoHandlerFound(this ILogger<ReactorInvoker> logger, ReactorId reactorId, string eventTypeName);

    [LoggerMessage(LogLevel.Warning, "Reactor {ReactorId} returned a value of type {ReturnTypeName} that no side-effect handler could process; the value was discarded.")]
    internal static partial void ReactorReturnValueNotHandled(this ILogger<ReactorInvoker> logger, ReactorId reactorId, string returnTypeName);
}
