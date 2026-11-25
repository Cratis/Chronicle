// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Represents a message from the client that belongs to one stream of a multiplexed observation connection.
/// </summary>
/// <remarks>
/// The first message of a stream opens it and carries the registration (<see cref="Reactor"/> holding a
/// <c language="csharp">RegisterReactor</c>, or <see cref="Reducer"/> holding a <c language="csharp">RegisterReducer</c>), exactly what the first message
/// of a per-observer call carries. Later messages carry results. <see cref="Close"/> ends the stream the way a client
/// ends a per-observer call by cancelling it.
/// </remarks>
[ProtoContract]
public class MultiplexedStreamMessage
{
    /// <summary>
    /// Gets or sets the identifier of the stream, chosen by the client and unique among the streams open on the connection.
    /// </summary>
    [ProtoMember(1)]
    public ulong StreamId { get; set; }

    /// <summary>
    /// Gets or sets what the stream observes with. Required on the message that opens the stream, ignored afterwards.
    /// </summary>
    [ProtoMember(2)]
    public MultiplexedObserverKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client closes the stream. A closing message carries no content.
    /// </summary>
    [ProtoMember(3)]
    public bool Close { get; set; }

    /// <summary>
    /// Gets or sets the message of a reactor stream.
    /// </summary>
    [ProtoMember(4)]
    public ReactorMessage? Reactor { get; set; }

    /// <summary>
    /// Gets or sets the message of a reducer stream.
    /// </summary>
    [ProtoMember(5)]
    public ReducerMessage? Reducer { get; set; }
}
