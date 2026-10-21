// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Logging for <see cref="ProjectionReplayHandler"/>.
/// </summary>
internal static partial class ProjectionReplayHandlerLogging
{
    [LoggerMessage(LogLevel.Warning, "Projection definition for observer {Key} is missing; replay cannot proceed")]
    internal static partial void ProjectionMissing(this ILogger<ProjectionReplayHandler> logger, ObserverKey key);
}
