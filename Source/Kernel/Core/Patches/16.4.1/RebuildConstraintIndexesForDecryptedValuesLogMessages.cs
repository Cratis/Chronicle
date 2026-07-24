// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Holds log messages for <see cref="RebuildConstraintIndexesForDecryptedValues"/>.
/// </summary>
internal static partial class RebuildConstraintIndexesForDecryptedValuesLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting RebuildConstraintIndexesForDecryptedValues patch - rebuilding unique constraint indexes from decrypted values")]
    internal static partial void StartingPatch(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger);

    [LoggerMessage(LogLevel.Debug, "Found {Count} unique constraint(s) for event store {EventStore}")]
    internal static partial void FoundUniqueConstraints(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger, EventStoreName eventStore, int count);

    [LoggerMessage(LogLevel.Debug, "Rebuilding constraint indexes for event store {EventStore}, namespace {Namespace}")]
    internal static partial void RebuildingConstraintIndexes(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Failed to start constraint index rebuild for event store {EventStore}, namespace {Namespace}: {Error}")]
    internal static partial void FailedRebuildingConstraintIndexes(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, string error);

    [LoggerMessage(LogLevel.Debug, "Completed RebuildConstraintIndexesForDecryptedValues patch")]
    internal static partial void PatchCompleted(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger);

    [LoggerMessage(LogLevel.Information, "RebuildConstraintIndexesForDecryptedValues cannot be rolled back - constraint values are hashed with SHA-256, which is a one-way transformation")]
    internal static partial void RollbackNotSupported(this ILogger<RebuildConstraintIndexesForDecryptedValues> logger);
}
