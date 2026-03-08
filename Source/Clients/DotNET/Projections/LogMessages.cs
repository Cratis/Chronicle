// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

internal static partial class LogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to create projection definition from projection of type {ProjectionType}")]
    internal static partial void FailedToCreateProjectionDefinition(this ILogger<Projections> logger, Type projectionType, Exception exception);
}
