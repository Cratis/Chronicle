// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

internal static partial class ReindexConstraintsStepLogMessages
{
    /// <summary>
    /// Warn that a historical claim was rejected because another event source already holds the value.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="constraintName">The constraint being rebuilt.</param>
    /// <param name="eventSourceId">The rejected claimant.</param>
    /// <param name="sequenceNumber">The rejected claim's event position.</param>
    /// <param name="error">The duplicate claim diagnostic.</param>
    [LoggerMessage(LogLevel.Warning, "Skipping duplicate claim for unique constraint {ConstraintName} from event source {EventSourceId} at sequence number {SequenceNumber} during reindex. The existing claim is preserved.")]
    internal static partial void SkippingDuplicateUniqueConstraintClaim(this ILogger<ReindexConstraintsStep> logger, ConstraintName constraintName, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, Exception error);

    /// <summary>
    /// Warn that an owner's existing facts are preserved because redaction removed an unrecoverable scope.
    /// </summary>
    /// <param name="logger">The job-step logger.</param>
    /// <param name="constraintName">The skipped constraint.</param>
    /// <param name="sequenceNumber">The unrecoverable transition's event position.</param>
    [LoggerMessage(LogLevel.Warning, "Skipping rebuild of closing constraint {ConstraintName} at sequence number {SequenceNumber}: the redacted property-sourced transition has no recoverable scope. Existing closure rows are left untouched.")]
    internal static partial void SkippingRedactedClosingConstraint(this ILogger<ReindexConstraintsStep> logger, ConstraintName constraintName, EventSequenceNumber sequenceNumber);
}
