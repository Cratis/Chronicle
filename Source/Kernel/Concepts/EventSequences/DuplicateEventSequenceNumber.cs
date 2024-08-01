// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Exception that gets thrown when a duplicate sequence number is detected.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DuplicateEventSequenceNumber"/> class.
/// </remarks>
/// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> that is duplicate.</param>
/// <param name="eventSequenceId">For which <see cref="EventSequenceId"/> the event was attempted appended to.</param>
public class DuplicateEventSequenceNumber(
    EventSequenceNumber sequenceNumber,
    EventSequenceId eventSequenceId) : Exception($"Duplicate sequence number: {sequenceNumber} when appending to event sequence: {eventSequenceId}. Will retry with new sequence number.")
{
}
