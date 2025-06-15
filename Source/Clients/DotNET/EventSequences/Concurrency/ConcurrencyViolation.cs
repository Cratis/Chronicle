// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency violation that occurred during an append operation.
/// </summary>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> that was violated.</param>
/// <param name="AttemptedEvent">The event that was attempted to be appended.</param>
public record ConcurrencyViolation(EventSequenceId EventSequenceId, object AttemptedEvent);
