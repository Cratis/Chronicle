// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Holds log messages for <see cref="ProjectionPipeline"/>.
/// </summary>
internal static partial class ProjectionPipelineLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting projection pipeline for event {SequenceNumber}")]
    internal static partial void StartingPipeline(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Error while performing projection pipeline step {StepType} for event {SequenceNumber}")]
    internal static partial void ErrorPerformingStep(this ILogger<ProjectionPipeline> logger, Exception ex, Type stepType, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Completed all projection pipeline steps for event {SequenceNumber}")]
    internal static partial void CompletedAllSteps(this ILogger<ProjectionPipeline> logger, ulong sequenceNumber);
}
