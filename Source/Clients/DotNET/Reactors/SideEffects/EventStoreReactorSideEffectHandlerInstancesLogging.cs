// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.SideEffects;

internal static partial class EventStoreReactorSideEffectHandlerInstancesLogging
{
    [LoggerMessage(LogLevel.Warning, "Discovered reactor side-effect handler {HandlerType} is not registered or cannot be constructed; skipping it")]
    internal static partial void DiscoveredHandlerNotRegistered(this ILogger<EventStoreReactorSideEffectHandlerInstances> logger, Type handlerType);
}
