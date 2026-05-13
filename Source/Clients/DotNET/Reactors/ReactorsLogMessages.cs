// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

internal static partial class ReactorsLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registering Reactor '{Id}'")]
    internal static partial void RegisteringReactor(this ILogger<Reactors> logger, ReactorId id);

    [LoggerMessage(LogLevel.Trace, "Discover all Reactors")]
    internal static partial void DiscoverAllReactors(this ILogger<Reactors> logger);

    [LoggerMessage(LogLevel.Trace, "Event of type {EventTypeId} was received for Reactor {ReactorId}")]
    internal static partial void EventReceived(this ILogger<Reactors> logger, EventTypeId eventTypeId, ReactorId reactorId);

    [LoggerMessage(LogLevel.Warning, "An error occurred while handling event of type {EventTypeId} was for Reactor {ReactorId}")]
    internal static partial void ErrorWhileHandlingEvent(this ILogger<Reactors> logger, Exception ex, EventTypeId eventTypeId, ReactorId reactorId);

    [LoggerMessage(LogLevel.Trace, "Handling of events received for Reactor {ReactorId} completed")]
    internal static partial void EventHandlingCompleted(this ILogger<Reactors> logger, ReactorId reactorId);

    [LoggerMessage(LogLevel.Debug, "Reactor observation stream for '{Id}' was cancelled")]
    internal static partial void RegisteringReactorStreamCancelled(this ILogger<Reactors> logger, ReactorId id, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to register Reactor '{Id}' — the reactive observation stream errored out")]
    internal static partial void RegisteringReactorFailed(this ILogger<Reactors> logger, ReactorId id, Exception exception);

    [LoggerMessage(LogLevel.Information, "Reconnecting Reactor '{Id}' after stream failure")]
    internal static partial void ReconnectingReactor(this ILogger<Reactors> logger, ReactorId id);
}
