// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Clients;

/// <summary>
/// The request for keeping the connection alive - sent by the client.
/// </summary>
[ProtoContract]
public class ConnectionKeepAlive
{
    /// <summary>
    /// Gets or sets the unique identifier of the connection.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; } = string.Empty;
}
