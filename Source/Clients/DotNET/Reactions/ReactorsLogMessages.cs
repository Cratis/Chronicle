// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

internal static partial class ReactorsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Registering Reactor '{Id}'")]
    internal static partial void RegisteringReactor(this ILogger<Reactors> logger, ReactorId id);

    [LoggerMessage(LogLevel.Trace, "Discover all Reactors")]
    internal static partial void DiscoverAllReactors(this ILogger<Reactors> logger);

    [LoggerMessage(LogLevel.Trace, "Event of type {EventTypeId} was received for Reactor {ReactorId}")]
    internal static partial void EventReceived(this ILogger<Reactors> logger, EventTypeId eventTypeId, ReactorId ReactorId);
}
