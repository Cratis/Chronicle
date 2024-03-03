// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Kernel.EventSequences;

/// <summary>
/// Exception that gets thrown when a duplicate sequence number is detected.
/// </summary>
public class DuplicateEventSequenceNumber : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEventSequenceNumber"/> class.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> that is duplicate.</param>
    /// <param name="eventSequenceId">For which <see cref="EventSequenceId"/> the event was attempted appended to.</param>
    public DuplicateEventSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSequenceId eventSequenceId)
        : base($"Duplicate sequence number: {sequenceNumber} when appending to event sequence: {eventSequenceId}. Will retry with new sequence number.")
    {
    }
}
