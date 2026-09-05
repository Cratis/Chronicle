// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Cuts;

/// <summary>
/// Represents an exact position on one event sequence a read-model cut is bound to.
/// </summary>
[ProtoContract]
public class EventSequenceCut
{
    /// <summary>
    /// Gets or sets the event sequence the position is on.
    /// </summary>
    [ProtoMember(1)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the exact position - inclusive - every selected read model is bound to.
    /// </summary>
    [ProtoMember(2)]
    public ulong Position { get; set; }
}
