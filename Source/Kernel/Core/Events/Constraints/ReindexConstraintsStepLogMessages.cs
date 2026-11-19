// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

internal static partial class ReindexConstraintsStepLogMessages
{
    /// <summary>
    /// Inform that a constraint index rebuild is starting.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="eventStore">The event store the rebuild runs in.</param>
    /// <param name="namespace">The namespace the rebuild runs in.</param>
    /// <param name="eventSequenceId">The event sequence whose constraint indexes are rebuilt.</param>
    /// <param name="constraintNames">The constraints being rebuilt, comma separated.</param>
    /// <param name="runId">The rebuild run.</param>
    [LoggerMessage(LogLevel.Information, "Constraint index rebuild {RunId} starting for constraints [{ConstraintNames}] on event sequence {EventSequenceId} in event store {EventStore}, namespace {Namespace}. Governed appends are refused until it completes.")]
    internal static partial void ConstraintIndexRebuildStarted(this ILogger<ReindexConstraintsStep> logger, string eventStore, string @namespace, EventSequenceId eventSequenceId, string constraintNames, Guid runId);

    /// <summary>
    /// Inform that a constraint index rebuild finished, whatever its outcome.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="eventStore">The event store the rebuild ran in.</param>
    /// <param name="namespace">The namespace the rebuild ran in.</param>
    /// <param name="eventSequenceId">The event sequence whose constraint indexes were rebuilt.</param>
    /// <param name="constraintNames">The constraints rebuilt, comma separated.</param>
    /// <param name="runId">The rebuild run.</param>
    /// <param name="succeeded">Whether the rebuild ran to completion.</param>
    /// <param name="duration">How long the rebuild took.</param>
    [LoggerMessage(LogLevel.Information, "Constraint index rebuild {RunId} finished for constraints [{ConstraintNames}] on event sequence {EventSequenceId} in event store {EventStore}, namespace {Namespace}. Completed: {Succeeded}, duration {Duration}.")]
    internal static partial void ConstraintIndexRebuildFinished(this ILogger<ReindexConstraintsStep> logger, string eventStore, string @namespace, EventSequenceId eventSequenceId, string constraintNames, Guid runId, bool succeeded, TimeSpan duration);

    /// <summary>
    /// Warn that the terminal handoff could not be delivered after its bounded retries.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="error">The final delivery failure.</param>
    [LoggerMessage(LogLevel.Warning, "Constraint index rebuild terminal handoff failed after five attempts. Rebuilding marks may remain until the event sequence activation restarts.")]
    internal static partial void ConstraintIndexRebuildHandoffFailed(this ILogger<ReindexConstraintsStep> logger, Exception error);

    /// <summary>
    /// Warn that cancellation could not confirm the conservative terminal handoff.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="error">The handoff failure.</param>
    [LoggerMessage(LogLevel.Warning, "Constraint index rebuild cancellation handoff failed. Its outcome remains unknown; rebuilding marks may remain until the event sequence activation restarts.")]
    internal static partial void ConstraintIndexRebuildCancellationHandoffFailed(this ILogger<ReindexConstraintsStep> logger, Exception error);

    /// <summary>
    /// Warn that a historical claim was rejected because another event source already holds the value.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="constraintName">The constraint being rebuilt.</param>
    /// <param name="sequenceNumber">The rejected claim's event position.</param>
    /// <param name="error">The duplicate claim diagnostic.</param>
    [LoggerMessage(LogLevel.Warning, "Skipping duplicate claim for unique constraint {ConstraintName} at sequence number {SequenceNumber} during reindex. The existing claim is preserved.")]
    internal static partial void SkippingDuplicateUniqueConstraintClaim(this ILogger<ReindexConstraintsStep> logger, ConstraintName constraintName, EventSequenceNumber sequenceNumber, Exception error);

    /// <summary>
    /// Warn that an owner's existing facts are preserved because redaction removed an unrecoverable scope.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="constraintName">The skipped constraint.</param>
    /// <param name="sequenceNumber">The unrecoverable transition's event position.</param>
    [LoggerMessage(LogLevel.Warning, "Skipping rebuild of closing constraint {ConstraintName} at sequence number {SequenceNumber}: the redacted property-sourced transition has no recoverable scope. Existing closure rows are left untouched.")]
    internal static partial void SkippingRedactedClosingConstraint(this ILogger<ReindexConstraintsStep> logger, ConstraintName constraintName, EventSequenceNumber sequenceNumber);
}
