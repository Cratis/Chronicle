// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Cuts;

/// <summary>
/// Represents an exact position on one event sequence a read-model cut is bound to.
/// </summary>
/// <param name="EventSequenceId">The <see cref="Concepts.EventSequences.EventSequenceId"/> the position is on.</param>
/// <param name="Position">The exact <see cref="EventSequenceNumber"/> - inclusive - every selected read model is bound to.</param>
/// <remarks>
/// A cut request carries a vector of these rather than a single global position, so dependencies spanning several
/// event sequences can each be pinned to their own exact position without inventing one global revision number
/// that does not exist across sequences.
/// </remarks>
public record EventSequenceCut(EventSequenceId EventSequenceId, EventSequenceNumber Position);
