// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the last appended sequence number for an event type.
/// </summary>
[ProtoContract]
public class AppendedEventTypeTail
{
    /// <summary>
    /// Gets or sets the type of the appended event.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the last appended event of this type.
    /// </summary>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }
}
