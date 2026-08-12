// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when events for different event sequences are enrolled in the same unit of work.
/// </summary>
/// <param name="enrolledEventSequenceId">The event sequence the unit of work is already bound to.</param>
/// <param name="attemptedEventSequenceId">The different event sequence that was attempted.</param>
public class UnitOfWorkCannotSpanEventSequences(EventSequenceId enrolledEventSequenceId, EventSequenceId attemptedEventSequenceId)
    : Exception($"A unit of work bound to event sequence '{enrolledEventSequenceId}' cannot enroll events for event sequence '{attemptedEventSequenceId}'.");
