// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Holds log messages for <see cref="ProjectionPipeline"/>.
    /// </summary>
    public static partial class ProjectionPipelineLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Handling event with sequence number {SequenceNumber}")]
        internal static partial void HandlingEvent(this ILogger logger, uint sequenceNumber);

        [LoggerMessage(1, LogLevel.Trace, "Getting initial values for event with sequence number {SequenceNumber}")]
        internal static partial void GettingInitialValues(this ILogger logger, uint sequenceNumber);

        [LoggerMessage(2, LogLevel.Trace, "Projecting for event with sequence number {SequenceNumber}")]
        internal static partial void Projecting(this ILogger logger, uint sequenceNumber);

        [LoggerMessage(3, LogLevel.Trace, "Saving result for event with sequence number {SequenceNumber}")]
        internal static partial void SavingResult(this ILogger logger, uint sequenceNumber);
        [LoggerMessage(4, LogLevel.Debug, "Pausing projection '{Projection}'")]
        internal static partial void Pausing(this ILogger logger, ProjectionId projection);

        [LoggerMessage(5, LogLevel.Debug, "Resuming projection '{Projection}'")]
        internal static partial void Resuming(this ILogger logger, ProjectionId projection);

        [LoggerMessage(6, LogLevel.Debug, "Rewinding projection '{Projection}'")]
        internal static partial void Rewinding(this ILogger logger, ProjectionId projection);

        [LoggerMessage(7, LogLevel.Debug, "Catching up projection '{Projection}' for result store configuration '{ConfigurationId}'")]
        internal static partial void CatchingUp(this ILogger logger, ProjectionId projection, ProjectionResultStoreConfigurationId configurationId);

        [LoggerMessage(8, LogLevel.Error, "Error starting projection '{Projection}'")]
        internal static partial void ErrorStartingProviding(this ILogger logger, ProjectionId projection, Exception exception);
    }
}
