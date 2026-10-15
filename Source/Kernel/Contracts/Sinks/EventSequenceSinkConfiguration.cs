// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Sinks;

/// <summary>
/// Represents additive event-sequence target metadata; it does not enable publication.
/// </summary>
[ProtoContract]
public class EventSequenceSinkConfiguration
{
    /// <summary>
    /// Gets or sets the exact registered event type, including generation.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the destination sequence. Omission selects the outbox.
    /// </summary>
    [ProtoMember(2)]
    public string? EventSequence { get; set; }

    /// <summary>
    /// Gets or sets whether the event type was declared public by the registering client.
    /// </summary>
    [ProtoMember(3)]
    public bool IsPublic { get; set; }
}
