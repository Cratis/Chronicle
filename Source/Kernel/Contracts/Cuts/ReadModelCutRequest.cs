// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Cuts;

/// <summary>
/// Represents a request to capture a selection of read models exactly at a vector of event-sequence cuts.
/// </summary>
[ProtoContract]
public class ReadModelCutRequest
{
    /// <summary>
    /// Gets or sets the event store the selection belongs to.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace the selection belongs to.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the exact position, per event sequence, every selected read model is bound to.
    /// </summary>
    [ProtoMember(3)]
    public IEnumerable<EventSequenceCut> Cuts { get; set; } = [];

    /// <summary>
    /// Gets or sets the read model identifiers to capture.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<string> Selection { get; set; } = [];
}
