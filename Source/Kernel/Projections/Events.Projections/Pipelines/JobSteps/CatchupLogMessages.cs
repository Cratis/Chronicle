// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines.JobSteps
{
    /// <summary>
    /// Holds log messages for <see cref="Catchup"/>.
    /// </summary>
    public static partial class CatchupLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Catching up projection '{Projection}' for result store configuration '{ConfigurationId}'")]
        internal static partial void CatchingUp(this ILogger logger, ProjectionId projection, ProjectionResultStoreConfigurationId configurationId);
    }
}
