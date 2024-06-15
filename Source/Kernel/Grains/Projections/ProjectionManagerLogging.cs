// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Projections;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ProjectionManagerLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Registering projection '{Name} ({Identifier})'")]
    internal static partial void Registering(this ILogger<ProjectionManager> logger, ProjectionId identifier, ProjectionName name);
}

internal static class ProjectionManagerScopes
{
    internal static IDisposable? BeginProjectionManagerScope(this ILogger<ProjectionManager> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["EventStore"] = eventStore,
            ["EventStoreNamespace"] = @namespace
        });
}
