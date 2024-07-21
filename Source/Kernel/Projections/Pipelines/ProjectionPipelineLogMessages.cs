// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Holds log messages for <see cref="ProjectionPipeline"/>.
/// </summary>
internal static partial class ProjectionPipelineLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Handling event with sequence number {SequenceNumber}")]
    internal static partial void HandlingEvent(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Getting initial values for event with sequence number {SequenceNumber}")]
    internal static partial void GettingInitialValues(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Projecting for event with sequence number {SequenceNumber}")]
    internal static partial void Projecting(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Saving result for event with sequence number {SequenceNumber}")]
    internal static partial void SavingResult(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Projection '{Id} - {Path}' is not accepting event of type '{EventType}' at sequence number {SequenceNumber}")]
    internal static partial void EventNotAccepted(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber, ProjectionId id, ProjectionPath path, EventType eventType);
}
