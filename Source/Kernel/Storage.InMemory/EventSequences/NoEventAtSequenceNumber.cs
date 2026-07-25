// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

/// <summary>
/// The exception that is thrown when an operation targets a sequence number that holds no event.
/// </summary>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> that was targeted.</param>
/// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> that holds no event.</param>
public class NoEventAtSequenceNumber(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber)
    : Exception($"There is no event at sequence number {sequenceNumber} in event sequence '{eventSequenceId}'.");
