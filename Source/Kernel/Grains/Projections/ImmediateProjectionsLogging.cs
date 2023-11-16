// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Projections;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ImmediateProjectionsLogMessages
{
    internal static partial void GettingModelInstance(this ILogger<ImmediateProjection> logger);
}

internal static class ImmediateProjectionScopes
{
    internal static IDisposable? BeginImmediateProjectionScope(this ILogger<ImmediateProjection> logger, ProjectionId projectionId, ImmediateProjectionKey projectionKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ProjectionId"] = projectionId,
            ["MicroserviceId"] = projectionKey.MicroserviceId,
            ["TenantId"] = projectionKey.TenantId,
            ["EventSequenceId"] = projectionKey.EventSequenceId
        });
}
