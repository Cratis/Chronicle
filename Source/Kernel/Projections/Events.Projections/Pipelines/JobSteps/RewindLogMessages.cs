// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines.JobSteps
{
    public static partial class RewindLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Rewinding projection '{Projection}' for configuration '{Configuration}'")]
        internal static partial void Rewinding(this ILogger logger, ProjectionId projection, ProjectionResultStoreConfigurationId configuration);
    }
}
