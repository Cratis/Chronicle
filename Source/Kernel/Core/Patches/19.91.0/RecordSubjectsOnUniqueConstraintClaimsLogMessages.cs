// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Holds log messages for <see cref="RecordSubjectsOnUniqueConstraintClaims"/>.
/// </summary>
internal static partial class RecordSubjectsOnUniqueConstraintClaimsLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting RecordSubjectsOnUniqueConstraintClaims patch - rebuilding unique constraint indexes to record the subject of each claim")]
    internal static partial void StartingPatch(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger);

    [LoggerMessage(LogLevel.Debug, "Found {Count} unique constraint(s) covering personal data for event store {EventStore}")]
    internal static partial void FoundUniqueConstraints(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger, EventStoreName eventStore, int count);

    [LoggerMessage(LogLevel.Debug, "Rebuilding constraint indexes for event store {EventStore}, namespace {Namespace}")]
    internal static partial void RebuildingConstraintIndexes(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Failed to start constraint index rebuild for event store {EventStore}, namespace {Namespace}: {Error}")]
    internal static partial void FailedRebuildingConstraintIndexes(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, string error);

    [LoggerMessage(LogLevel.Debug, "Completed RecordSubjectsOnUniqueConstraintClaims patch")]
    internal static partial void PatchCompleted(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger);

    [LoggerMessage(LogLevel.Information, "RecordSubjectsOnUniqueConstraintClaims cannot be rolled back - claims of erased subjects are not restored")]
    internal static partial void RollbackNotSupported(this ILogger<RecordSubjectsOnUniqueConstraintClaims> logger);
}
