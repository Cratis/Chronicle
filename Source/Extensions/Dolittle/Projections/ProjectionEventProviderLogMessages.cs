// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Holds log messages for <see cref="ProjectionEventProvider"/>.
    /// </summary>
    public static partial class ProjectionEventProviderLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Providing events for projection '{Projection}'")]
        internal static partial void ProvidingFor(this ILogger logger, ProjectionId projection);

        [LoggerMessage(1, LogLevel.Debug, "Pausing projection '{Projection}'")]
        internal static partial void Pausing(this ILogger logger, ProjectionId projection);

        [LoggerMessage(2, LogLevel.Debug, "Resuming projection '{Projection}'")]
        internal static partial void Resuming(this ILogger logger, ProjectionId projection);

        [LoggerMessage(3, LogLevel.Debug, "Rewinding projection '{Projection}'")]
        internal static partial void Rewinding(this ILogger logger, ProjectionId projection);

        [LoggerMessage(4, LogLevel.Debug, "Catching up projection '{Projection}'")]
        internal static partial void CatchingUp(this ILogger logger, ProjectionId projection);

        [LoggerMessage(5, LogLevel.Trace, "Getting events from offset {Offset}")]
        internal static partial void GettingEventsFromOffset(this ILogger logger, uint offset);

        [LoggerMessage(6, LogLevel.Trace, "Providing event with sequence number {SequenceNumber}")]
        internal static partial void ProvidingEvent(this ILogger logger, uint sequenceNumber);

        [LoggerMessage(7, LogLevel.Information, "Projection '{Projection}' is not interested in any event types, skipping catch up.")]
        internal static partial void SkippingProvidingForProjectionDueToNoEventTypes(this ILogger logger, ProjectionId projection);

        [LoggerMessage(8, LogLevel.Error, "Error during starting projection '{Projection}'")]
        internal static partial void ErrorStartingProviding(this ILogger logger, ProjectionId projection, Exception exception);
    }
}
