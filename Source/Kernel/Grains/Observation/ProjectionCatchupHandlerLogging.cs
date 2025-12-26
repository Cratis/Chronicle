// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Holds log messages for <see cref="ProjectionCatchupHandler"/>.
/// </summary>
internal static partial class ProjectionCatchupHandlerLogging
{
    [LoggerMessage(LogLevel.Error, "Failed to handle catchup for observer '{ObserverId}' of type '{Type}'")]
    internal static partial void FailedToHandleCatchup(this ILogger<ProjectionCatchupHandler> logger, Exception exception, ObserverId observerId, ObserverType type);
}
