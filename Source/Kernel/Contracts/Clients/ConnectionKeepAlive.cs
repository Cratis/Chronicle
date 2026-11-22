// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// The request for keeping the connection alive - sent by the client.
/// </summary>
/// <remarks>
/// It is also what the server pushes on the connection stream; there it carries the <see cref="Status"/> of the server.
/// </remarks>
[ProtoContract]
public class ConnectionKeepAlive
{
    /// <summary>
    /// Gets or sets the unique identifier of the connection.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how busy the server is. Set by the server on the keep-alives it pushes; a client leaves it out,
    /// and a server that does not know about it leaves it at <see cref="KernelStatus.Accepting"/>.
    /// </summary>
    [ProtoMember(2)]
    public KernelStatus Status { get; set; } = KernelStatus.Accepting;
}
