// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;


namespace Cratis.Events.Store.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract for committing events through the <see cref="IEventLogService"/>.
    /// </summary>
    [ProtoContract]
    public class CommitRequest
    {
        /// <summary>
        /// Gets or inits the unique identifier for the event log to commit to.
        /// </summary>
        [ProtoMember(1)]
        public Guid EventLogId { get; init; }

        /// <summary>
        /// Gets or inits the unique identifier of the event source - typically the primary key.
        /// </summary>
        [ProtoMember(2)]
        public string EventSourceId { get; init; }

        /// <summary>
        /// Gets or inits the <see cref="EventType">type of event</see>.
        /// </summary>
        [ProtoMember(3)]
        public EventType EventType { get; init; }

        /// <summary>
        /// Gets or inits the payload in the form of a string representation of the JSON.
        /// </summary>
        [ProtoMember(4)]
        public string Content { get; init; }
    }
}
