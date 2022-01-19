// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Holds log messages for <see cref="ProjectionPipeline"/>.
    /// </summary>
    public static partial class ProjectionPipelineLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Starting projection '{Projection}'")]
        internal static partial void Starting(this ILogger logger, ProjectionId projection);

        [LoggerMessage(1, LogLevel.Debug, "Pausing projection '{Projection}'")]
        internal static partial void Pausing(this ILogger logger, ProjectionId projection);

        [LoggerMessage(2, LogLevel.Debug, "Resuming projection '{Projection}'")]
        internal static partial void Resuming(this ILogger logger, ProjectionId projection);

        [LoggerMessage(3, LogLevel.Debug, "Rewinding projection '{Projection}'")]
        internal static partial void Rewinding(this ILogger logger, ProjectionId projection);

        [LoggerMessage(4, LogLevel.Debug, "Rewinding projection '{Projection}' for configuration '{Configuration}'")]
        internal static partial void RewindingForConfiguration(this ILogger logger, ProjectionId projection, ProjectionResultStoreConfigurationId configuration);

        [LoggerMessage(5, LogLevel.Warning, "Projection '{Projection}' is being suspended with the reason '{reason}'")]
        internal static partial void Suspended(this ILogger logger, ProjectionId projection, string reason);
    }
}
