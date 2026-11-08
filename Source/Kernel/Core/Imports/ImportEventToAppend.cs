// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents a staged import event handed to the event sequence, with what it keeps from its source.
/// </summary>
/// <param name="Index">The index of the event in the import.</param>
/// <param name="Event">The <see cref="EventToAppend"/>, its causation already carrying the import.</param>
/// <param name="IdempotencyKey">The idempotency key from the source, if any.</param>
/// <param name="CorrelationId">The correlation identifier from the source, if any.</param>
/// <param name="CausedBy">The identity that caused the event in the source, if any.</param>
public record ImportEventToAppend(
    long Index,
    EventToAppend Event,
    IdempotencyKey? IdempotencyKey,
    CorrelationId? CorrelationId,
    Identity? CausedBy);
