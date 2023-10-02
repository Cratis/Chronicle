// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Clients;

/// <summary>
/// The request for connecting.
/// </summary>
[ProtoContract]
public class ConnectRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the connection.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    [ProtoMember(2)]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether or not the client is running with debugger attached.
    /// </summary>
    [ProtoMember(3)]
    public bool IsRunningWithDebugger { get; set; }
}
