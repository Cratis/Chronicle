// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Holds log messages for <see cref="RebuildConstraintIndexes"/>.
/// </summary>
internal static partial class RebuildConstraintIndexesLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting RebuildConstraintIndexes patch - rebuilding unique constraint indexes with hashed values")]
    internal static partial void StartingPatch(this ILogger<RebuildConstraintIndexes> logger);

    [LoggerMessage(LogLevel.Debug, "Found {Count} unique constraint(s) for event store {EventStore}")]
    internal static partial void FoundUniqueConstraints(this ILogger<RebuildConstraintIndexes> logger, EventStoreName eventStore, int count);

    [LoggerMessage(LogLevel.Debug, "Rebuilding constraint indexes for event store {EventStore}, namespace {Namespace}")]
    internal static partial void RebuildingConstraintIndexes(this ILogger<RebuildConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Failed to start constraint index rebuild for event store {EventStore}, namespace {Namespace}: {Error}")]
    internal static partial void FailedRebuildingConstraintIndexes(this ILogger<RebuildConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, string error);

    [LoggerMessage(LogLevel.Debug, "Completed RebuildConstraintIndexes patch")]
    internal static partial void PatchCompleted(this ILogger<RebuildConstraintIndexes> logger);

    [LoggerMessage(LogLevel.Information, "RebuildConstraintIndexes cannot be rolled back - constraint values are hashed with SHA-256, which is a one-way transformation")]
    internal static partial void RollbackNotSupported(this ILogger<RebuildConstraintIndexes> logger);
}
