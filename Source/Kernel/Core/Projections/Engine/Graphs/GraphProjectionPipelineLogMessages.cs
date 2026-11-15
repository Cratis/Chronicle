// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

internal static partial class GraphProjectionPipelineLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Graph projection has nothing to apply for event {EventSequenceNumber}")]
    internal static partial void NothingToApply(this ILogger<GraphProjectionPipeline> logger, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Applied {OperationCount} graph operations for event {EventSequenceNumber}")]
    internal static partial void AppliedOperations(this ILogger<GraphProjectionPipeline> logger, int operationCount, EventSequenceNumber eventSequenceNumber);
}
