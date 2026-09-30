// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Holds log messages for <see cref="ReindexConstraintsStep"/>.
/// </summary>
internal static partial class ReindexConstraintsStepLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Constraint {ConstraintName} kept an existing claim while reindexing event sequence {EventSequenceId}: the value of the event at {SequenceNumber} is already claimed by another event source")]
    internal static partial void SkippedDuplicateWhileReindexing(this ILogger<ReindexConstraintsStep> logger, ConstraintName constraintName, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);
}
