// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Represents a message from the kernel that belongs to one stream of a multiplexed observation connection.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Reactor"/>, <see cref="Reducer"/> and <see cref="End"/> is set. Messages of one stream
/// arrive in the order the kernel produced them, which <see cref="Sequence"/> lets a client verify.
/// </remarks>
[ProtoContract]
public class MultiplexedServerMessage
{
    /// <summary>
    /// Gets or sets the identifier of the stream the message belongs to, as the client chose it.
    /// </summary>
    [ProtoMember(1)]
    public ulong StreamId { get; set; }

    /// <summary>
    /// Gets or sets the position of the message in its stream, starting at 1 and increasing by 1 for each message including the end.
    /// </summary>
    [ProtoMember(2)]
    public ulong Sequence { get; set; }

    /// <summary>
    /// Gets or sets the events of a reactor stream, what a per-observer call would have emitted.
    /// </summary>
    [ProtoMember(3)]
    public EventsToObserve? Reactor { get; set; }

    /// <summary>
    /// Gets or sets the operation of a reducer stream, what a per-observer call would have emitted.
    /// </summary>
    [ProtoMember(4)]
    public ReduceOperationMessage? Reducer { get; set; }

    /// <summary>
    /// Gets or sets the end of the stream, set when the stream is over.
    /// </summary>
    [ProtoMember(5)]
    public MultiplexedStreamEnd? End { get; set; }
}
