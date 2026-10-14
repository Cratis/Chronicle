// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

internal static partial class ConstraintIndexReplayLogging
{
    [LoggerMessage(LogLevel.Error, "Constraint {Constraint} cannot resolve stored event {Position}; settling its effect with error")]
    internal static partial void UnresolvedReplayEvent(this ILogger<ConstraintIndexReplay> logger, ConstraintName constraint, EventSequenceNumber position, Exception error);

    [LoggerMessage(LogLevel.Warning, "Constraint {Constraint} cannot recover redacted property scope at {Position}; settling its effect with error")]
    internal static partial void UnrecoverableRedactedScope(this ILogger<ConstraintIndexReplay> logger, ConstraintName constraint, EventSequenceNumber position, Exception error);

    [LoggerMessage(LogLevel.Error, "Constraint {Constraint} could not read replay input at {Position}; the constraint remains pending")]
    internal static partial void FailedReplayRead(this ILogger<ConstraintIndexReplay> logger, ConstraintName constraint, EventSequenceNumber? position, Exception error);

    [LoggerMessage(LogLevel.Error, "Constraint {Constraint} could not repair event {Position}; the constraint remains pending")]
    internal static partial void FailedReplayWrite(this ILogger<ConstraintIndexReplay> logger, ConstraintName constraint, EventSequenceNumber position, Exception error);
}
