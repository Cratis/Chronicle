// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps
{
    /// <summary>
    /// Holds log messages for <see cref="Catchup"/>.
    /// </summary>
    public static partial class CatchupLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Catching up projection '{Projection}' for sink configuration '{ConfigurationId}'")]
        internal static partial void CatchingUp(this ILogger logger, ProjectionId projection, ProjectionSinkConfigurationId configurationId);
    }
}
