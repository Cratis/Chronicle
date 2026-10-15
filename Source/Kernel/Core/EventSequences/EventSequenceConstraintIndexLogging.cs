// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

internal static partial class EventSequenceConstraintIndexLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Constraint {Constraint} on {Sequence} had index outcome {Outcome} at {Position}, watermark {Watermark}, exclusive tail {Tail}")]
    internal static partial void ConstraintIndexUpdateOutcome(this ILogger<EventSequence> logger, EventSequenceKey sequence, ConstraintName constraint, EventSequenceNumber position, EventSequenceNumber watermark, EventSequenceNumber tail, ConstraintIndexUpdateOutcome outcome, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Constraint {Constraint} on {Sequence} has an unconfirmed catch-up write below {Boundary}, watermark {Watermark}, exclusive tail {Tail}")]
    internal static partial void ConstraintIndexCatchUpAbandoned(this ILogger<EventSequence> logger, EventSequenceKey sequence, ConstraintName constraint, EventSequenceNumber boundary, EventSequenceNumber watermark, EventSequenceNumber tail);

    [LoggerMessage(Level = LogLevel.Error, Message = "Constraint {Constraint} on {Sequence} settled poison event {Position} after five identical non-transient failures; reindex after fixing the cause")]
    internal static partial void ConstraintIndexPoisonEvent(this ILogger<EventSequence> logger, EventSequenceKey sequence, ConstraintName constraint, EventSequenceNumber position, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Constraint indexes on {Sequence} were repaired but the snapshot at watermark {Watermark} was not persisted")]
    internal static partial void ConstraintIndexRepairSnapshotNotPersisted(this ILogger<EventSequence> logger, EventSequenceKey sequence, EventSequenceNumber watermark, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Constraint index repair failed on {Sequence}, watermark {Watermark}, exclusive tail {Tail}")]
    internal static partial void ConstraintIndexRepairFailed(this ILogger<EventSequence> logger, EventSequenceKey sequence, EventSequenceNumber watermark, EventSequenceNumber tail, Exception exception);
}
