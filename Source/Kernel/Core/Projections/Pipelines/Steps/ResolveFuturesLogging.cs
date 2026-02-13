// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
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

    [LoggerMessage(LogLevel.Debug, "Checking if parent exists in current state for future {FutureId}")]
    internal static partial void CheckingParentInCurrentState(this ILogger<ResolveFutures> logger, ProjectionFutureId futureId);

    [LoggerMessage(LogLevel.Debug, "Parent does not exist in current state for future {FutureId}, skipping")]
    internal static partial void ParentNotInCurrentState(this ILogger<ResolveFutures> logger, ProjectionFutureId futureId);

    [LoggerMessage(LogLevel.Debug, "Parent exists in current state for future {FutureId}, resolving key")]
    internal static partial void ParentExistsInCurrentState(this ILogger<ResolveFutures> logger, ProjectionFutureId futureId);

    [LoggerMessage(LogLevel.Debug, "Key resolution deferred for future {FutureId}")]
    internal static partial void KeyResolutionDeferred(this ILogger<ResolveFutures> logger, ProjectionFutureId futureId);

    [LoggerMessage(LogLevel.Debug, "Resolved key {Key} for future {FutureId}, calling OnNext")]
    internal static partial void ResolvedKeyCallingOnNext(this ILogger<ResolveFutures> logger, Key key, ProjectionFutureId futureId);

    [LoggerMessage(LogLevel.Debug, "Found child projection at path {ProjectionPath} for future {FutureId}")]
    internal static partial void FoundChildProjection(this ILogger<ResolveFutures> logger, string projectionPath, ProjectionFutureId futureId);
}
