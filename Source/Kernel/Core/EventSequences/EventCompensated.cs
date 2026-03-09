// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// Represents the fact that an event in a user sequence has been compensated.
/// When a user-sequence event is compensated, a new entry is appended to its <c>Compensations</c> array
/// in storage without changing the event's type. This record is a companion concept to
/// <see cref="EventCompensationRequested"/> (the system event that triggers the operation) and is useful
/// for tracing and logging.
/// </summary>
/// <remarks>
/// This is NOT a Chronicle system event (<c>[EventType]</c> is intentionally absent) — it is a concept
/// record for conveying compensation identity in tracing scenarios.
/// See also: <see cref="EventCompensationRequested"/>, which is the <c>[EventType]</c> system event that
/// triggers the reactor to perform the actual in-place update.
/// </remarks>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the compensated event.</param>
/// <param name="EventType">The <see cref="EventType"/> of the compensated event.</param>
public record EventCompensated(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    EventType EventType);
