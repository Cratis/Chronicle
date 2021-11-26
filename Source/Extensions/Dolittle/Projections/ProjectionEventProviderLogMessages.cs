// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Holds log messages for <see cref="ProjectionEventProvider"/>.
    /// </summary>
    public static partial class ProjectionEventProviderLogMessages
    {
        [LoggerMessage(0, LogLevel.Debug, "Providing events for projection '{Projection}'")]
        internal static partial void ProvidingFor(this ILogger logger, ProjectionId projection);

        [LoggerMessage(1, LogLevel.Information, "Projection '{Projection}' is not interested in any event types, skipping catch up.")]
        internal static partial void SkippingProvidingForProjectionDueToNoEventTypes(this ILogger logger, ProjectionId projection);

    }
}
