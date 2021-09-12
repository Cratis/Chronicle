// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Events.Store.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract for the commit result from <see cref="IEventLogService"/>.
    /// </summary>
    [ProtoContract]
    public class CommitResult
    {
        /// <summary>
        /// Gets or inits whether or not the commit was successful.
        /// </summary>
        [ProtoMember(1)]
        public bool Success { get; init; }
    }
}
