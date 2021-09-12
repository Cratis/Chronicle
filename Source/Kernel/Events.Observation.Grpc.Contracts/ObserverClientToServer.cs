// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;


namespace Cratis.Events.Observation.Grpc.Contracts
{
    /// <summary>
    /// Represents the protobuf message contract related to <see cref="IObserversService"/> coming from the client.
    /// </summary>
    /// <remarks>
    /// The message is used both for the initial subscription setup and the subsequent responses from the client when
    /// an event has been observed. The properties are therefor optional depending on the usecase they're used in.
    /// </remarks>
    [ProtoContract]
    public class ObserverClientToServer
    {
        /// <summary>
        /// Gets the optional <see cref="ObserverSubscribe">subscription</see>.
        /// </summary>
        [ProtoMember(1)]
        public ObserverSubscribe Subscription { get; init; }

        /// <summary>dd
        /// Gets the optional <see cref="ObserverResult">result of an observation</see>.
        /// </summary>
        [ProtoMember(2)]
        public ObserverResult Result { get; init; }
    }
}
