// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Events.Observation.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract used when sending items for observations to the client.
    /// </summary>
    [ProtoContract]
    public class ObserverServerToClient
    {
        [ProtoMember(1)]
        public Guid EventTypeId { get; init; }

        [ProtoMember(2)]
        public DateTime Occurred { get; init; }

        [ProtoMember(3)]
        public string Content { get; init; }
    }
}
