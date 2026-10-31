// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes a retry that could not be reconciled with its durable intent.
/// </summary>
/// <param name="Key">The caller's key.</param>
/// <param name="ExistingSequenceNumber">The durable event's slot.</param>
/// <param name="StoredEventType">The original type and generation, retained after redaction.</param>
/// <param name="RequestedGeneration">The retry's requested generation.</param>
/// <param name="Reason">The comparison refusal.</param>
public record IdempotencyConflict(IdempotencyKey Key, EventSequenceNumber ExistingSequenceNumber, EventType StoredEventType, EventTypeGeneration RequestedGeneration, IdempotencyConflictReason Reason);
