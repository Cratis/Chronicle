// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines
{
    public static partial class ProjectionPipelineHandlerLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Handling event with sequence number {SequenceNumber}")]
        internal static partial void HandlingEvent(this ILogger logger, ulong sequenceNumber);

        [LoggerMessage(1, LogLevel.Trace, "Getting initial values for event with sequence number {SequenceNumber}")]
        internal static partial void GettingInitialValues(this ILogger logger, ulong sequenceNumber);

        [LoggerMessage(2, LogLevel.Trace, "Projecting for event with sequence number {SequenceNumber}")]
        internal static partial void Projecting(this ILogger logger, ulong sequenceNumber);

        [LoggerMessage(3, LogLevel.Trace, "Saving result for event with sequence number {SequenceNumber}")]
        internal static partial void SavingResult(this ILogger logger, ulong sequenceNumber);
    }
}
