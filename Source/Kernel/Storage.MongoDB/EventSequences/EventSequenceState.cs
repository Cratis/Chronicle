// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the persisted state of an event sequence for MongoDB.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> representing the tail of the sequence.</param>
/// <param name="TailSequenceNumberPerEventType">Tail <see cref="EventSequenceNumber"/> per event type id.</param>
/// <param name="ConstraintIndexWatermark">Exclusive position of settled constraint updates, or null before coverage was recorded.</param>
/// <param name="ConstraintIndexCatchUpBelow">Exclusive boundary of pending unique-constraint catch-up.</param>
/// <param name="ConstraintIndexCatchUpRefusing">Constraint names whose catch-up writes require append refusal.</param>
/// <param name="MutationEpoch">Mutation epoch advanced by every revision and redaction, or null before one was recorded.</param>
public record EventSequenceState(
    EventSequenceNumber SequenceNumber,
    IDictionary<string, EventSequenceNumber> TailSequenceNumberPerEventType,
    EventSequenceNumber? ConstraintIndexWatermark = null,
    EventSequenceNumber? ConstraintIndexCatchUpBelow = null,
    IEnumerable<string>? ConstraintIndexCatchUpRefusing = null,
    ulong? MutationEpoch = null);
