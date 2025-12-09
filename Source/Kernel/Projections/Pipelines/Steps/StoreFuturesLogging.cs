// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

internal static partial class StoreFuturesLogging
{
    [LoggerMessage(LogLevel.Debug, "Stored future {FutureId} for projection {ProjectionId}")]
    internal static partial void StoredFuture(this ILogger<StoreFutures> logger, ProjectionFutureId futureId, ProjectionId projectionId);
}
