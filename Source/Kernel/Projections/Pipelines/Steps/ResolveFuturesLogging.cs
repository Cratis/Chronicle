// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

internal static partial class ResolveFuturesLogging
{
    [LoggerMessage(LogLevel.Warning, "HandleEvent step not set for ResolveFutures step - cannot resolve pending futures")]
    internal static partial void HandleEventStepNotSet(this ILogger<ResolveFutures> logger);

    [LoggerMessage(LogLevel.Debug, "Found {Count} pending futures for projection {ProjectionId}")]
    internal static partial void FoundPendingFutures(this ILogger<ResolveFutures> logger, int count, ProjectionId projectionId);

    [LoggerMessage(LogLevel.Information, "Resolved future {FutureId} for projection {ProjectionId}")]
    internal static partial void ResolvedFuture(this ILogger<ResolveFutures> logger, ProjectionFutureId futureId, ProjectionId projectionId);

    [LoggerMessage(LogLevel.Warning, "Failed to resolve future {FutureId} for projection {ProjectionId}")]
    internal static partial void FailedToResolveFuture(this ILogger<ResolveFutures> logger, Exception exception, ProjectionFutureId futureId, ProjectionId projectionId);
}
