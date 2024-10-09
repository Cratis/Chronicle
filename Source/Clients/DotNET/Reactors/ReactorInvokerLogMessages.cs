// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

internal static partial class ReactorInvokerLogMessages
{
    [LoggerMessage(LogLevel.Error, "Reactor of type '{ReactorId}' failed for event with type '{EventType}'")]
    internal static partial void ReactorFailed(this ILogger<ReactorInvoker> logger, ReactorId ReactorId, string eventType, Exception exception);
}
