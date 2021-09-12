// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Events.Store.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract of the definition of a type of event.
    /// </summary>
    [ProtoContract]
    public class EventType
    {
        /// <summary>
        /// Gets or inits the unique identifier representing the event type.
        /// </summary>
        [ProtoMember(1)]
        public Guid EventTypeId { get; init; }

        /// <summary>
        /// Gets or inits the generation of the event type.
        /// </summary>
        [ProtoMember(2)]
        public uint Generation { get; init; }
    }
}
