// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an appended event with a sequence number.
/// </summary>
public class AppendedEventWithSequenceNumber
{
    /// <summary>
    /// Gets the event.
    /// </summary>
    public AppendedEvent Event { get; init; } = null!;

    /// <summary>
    /// Gets the sequence number.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; init; } = EventSequenceNumber.First;
}
