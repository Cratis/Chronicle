// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Holds log messages for <see cref="RederiveUniqueConstraintIndexes"/>.
/// </summary>
internal static partial class RederiveUniqueConstraintIndexesLogMessages
{
    [LoggerMessage(LogLevel.Information, "Rebuilding unique constraint indexes for event store {EventStore}, namespace {Namespace}, event sequence {EventSequenceId}: {Pending} claims are pending")]
    internal static partial void RederivingConstraintIndexes(this ILogger<RederiveUniqueConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, int pending);

    [LoggerMessage(LogLevel.Warning, "Failed to start the unique constraint index rebuild for event store {EventStore}, namespace {Namespace}, event sequence {EventSequenceId}: {Error}")]
    internal static partial void FailedRederivingConstraintIndexes(this ILogger<RederiveUniqueConstraintIndexes> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, string error);

    [LoggerMessage(LogLevel.Debug, "Completed RederiveUniqueConstraintIndexes patch")]
    internal static partial void PatchCompleted(this ILogger<RederiveUniqueConstraintIndexes> logger);
}
