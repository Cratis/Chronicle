// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Events.Observation.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract result of an observation.
    /// </summary>
    [ProtoContract]
    public class ObserverResult
    {
        /// <summary>
        /// Gets or inits whether or not the observation was successful.
        /// </summary>
        [ProtoMember(1)]
        public bool Failed { get; init; }

        /// <summary>
        /// Gets or inits the reason for failing in case of a failure.
        /// </summary>
        [ProtoMember(2)]
        public string Reason { get; init; } = string.Empty;
    }
}
