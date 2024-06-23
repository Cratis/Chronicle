// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverInvokerLogMessages
{
    [LoggerMessage(LogLevel.Error, "Observer of type '{ObserverName}' failed for event with type '{EventType}'")]
    internal static partial void ObserverFailed(this ILogger<ObserverInvoker> logger, string observerName, string eventType, Exception exception);
}
