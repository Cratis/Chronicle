// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Represents one write from the client on a multiplexed observation connection.
/// </summary>
/// <remarks>
/// A write carries the messages of any number of streams, which is how registering many observers is one call
/// instead of one per observer: a client that has N observers to register puts the N registrations in one write.
/// </remarks>
[ProtoContract]
public class MultiplexedClientMessage
{
    /// <summary>
    /// Gets or sets the messages, in the order the client produced them. Messages of one stream are applied in this order.
    /// </summary>
    [ProtoMember(1)]
    public IList<MultiplexedStreamMessage> Messages { get; set; } = [];
}
