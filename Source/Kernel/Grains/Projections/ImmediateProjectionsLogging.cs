// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Projections;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ImmediateProjectionsLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Getting model instance for projection.")]
    internal static partial void GettingModelInstance(this ILogger<ImmediateProjection> logger);

    [LoggerMessage(1, LogLevel.Trace, "Using cached model instance for projection.")]
    internal static partial void UsingCachedModelInstance(this ILogger<ImmediateProjection> logger);

    [LoggerMessage(2, LogLevel.Trace, "No event types for projections, returning empty.")]
    internal static partial void NoEventTypes(this ILogger<ImmediateProjection> logger);

    [LoggerMessage(4, LogLevel.Error, "Failed getting model instance for projection.")]
    internal static partial void FailedGettingModelInstance(this ILogger<ImmediateProjection> logger, Exception exception);
}

internal static class ImmediateProjectionScopes
{
    internal static IDisposable? BeginImmediateProjectionScope(this ILogger<ImmediateProjection> logger, ProjectionId projectionId, ImmediateProjectionKey projectionKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ProjectionId"] = projectionId,
            ["MicroserviceId"] = projectionKey.MicroserviceId,
            ["TenantId"] = projectionKey.TenantId,
            ["EventSequenceId"] = projectionKey.EventSequenceId,
            ["SessionId"] = projectionKey.CorrelationId ?? string.Empty
        });
}
