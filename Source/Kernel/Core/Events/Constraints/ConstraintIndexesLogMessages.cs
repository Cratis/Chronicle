// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Holds log messages for <see cref="ConstraintIndexes"/>.
/// </summary>
internal static partial class ConstraintIndexesLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting constraint reindex for event store {EventStore}, namespace {Namespace}, event sequence {EventSequenceId}")]
    internal static partial void StartingReindex(this ILogger<ConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Warning, "Failed to start constraint reindex for event store {EventStore}, namespace {Namespace}, event sequence {EventSequenceId}: {Error}")]
    internal static partial void FailedStartingReindex(this ILogger<ConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, string error);

    [LoggerMessage(LogLevel.Warning, "Failed refreshing the constraints of event store {EventStore}, namespace {Namespace}, event sequence {EventSequenceId} before reindexing it")]
    internal static partial void FailedRefreshingConstraints(this ILogger<ConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed rebuilding stale constraint indexes for event store {EventStore}")]
    internal static partial void FailedRebuildingStaleIndexesForEventStore(this ILogger<ConstraintIndexes> logger, EventStoreName eventStore, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed rebuilding stale constraint indexes for event store {EventStore}, namespace {Namespace}")]
    internal static partial void FailedRebuildingStaleIndexes(this ILogger<ConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, Exception exception);
}
