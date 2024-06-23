// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Holds log messages for <see cref="Projection"/>.
/// </summary>
internal static partial class ProjectionLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Running projection pipeline with identifier '{Identifier}' - name '{Name}'")]
    internal static partial void Running(this ILogger<Projection> logger, ProjectionId identifier, ProjectionName name);

    [LoggerMessage(LogLevel.Debug, "Stopping the running og projection pipeline with identifier '{Identifier}' - name '{Name}'")]
    internal static partial void StopRunning(this ILogger<Projection> logger, ProjectionId identifier, ProjectionName name);
}
