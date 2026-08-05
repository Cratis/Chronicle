// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

internal static partial class LogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to create projection definition from projection of type {ProjectionType}")]
    internal static partial void FailedToCreateProjectionDefinition(this ILogger<Projections> logger, Type projectionType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to create projection definition from the model-bound attributes on read model {ReadModelType} - it will not be registered, and its read model will never update")]
    internal static partial void FailedToCreateModelBoundProjectionDefinition(this ILogger<Projections> logger, Type readModelType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "More than one projection maintains read model {ReadModelType}; {ShadowedProjection} is not reachable by that read model because {ReachableProjection} claimed it first. Both are still registered, so both write to it")]
    internal static partial void MoreThanOneProjectionForReadModel(this ILogger<Projections> logger, Type readModelType, string shadowedProjection, string reachableProjection);
}
