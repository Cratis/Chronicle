// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Events.Observation.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract for setting up an observer subscription.
    /// </summary>
    [ProtoContract]
    public class ObserverSubscribe
    {
        /// <summary>
        /// Gets or inits the eventlog the subscription is for.
        /// </summary>
        [ProtoMember(1)]
        public Guid EventLogId { get; init; }

        /// <summary>
        /// Gets or inits the unique identifier of the subscription.
        /// </summary>
        [ProtoMember(2)]
        public Guid Id { get; init; }
    }
}
