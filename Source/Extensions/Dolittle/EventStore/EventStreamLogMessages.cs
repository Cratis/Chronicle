// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Holds log messages for <see cref="EventStream"/>.
    /// </summary>
    public static partial class EventStreamLogMessages
    {
        [LoggerMessage(0, LogLevel.Trace, "Getting events from offset {Offset}")]
        internal static partial void GettingEventsFromOffset(this ILogger logger, uint offset);
    }
}
