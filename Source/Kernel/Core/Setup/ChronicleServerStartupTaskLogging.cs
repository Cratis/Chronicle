// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Holds log messages for <see cref="ChronicleServerStartupTask"/>.
/// </summary>
internal static partial class ChronicleServerStartupTaskLogging
{
    [LoggerMessage(LogLevel.Warning, "Skipping persisted projection definition '{Identifier}' during startup because the current engine rejected it. Chronicle will continue starting so a client can re-register the projection with its current definition")]
    internal static partial void FailedRegisteringPersistedProjectionDefinition(this ILogger<ChronicleServerStartupTask> logger, Exception exception, ProjectionId identifier);
}
