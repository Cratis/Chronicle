// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Represents the information related to a connected client.
/// </summary>
[ProtoContract]
public class ConnectedClient
{
    /// <summary>
    /// Gets the unique connection id.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version of the client.
    /// </summary>
    [ProtoMember(2)]
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time the client was last seen.
    /// </summary>
    [ProtoMember(3)]
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>
    /// Gets whether or not the client is running with debugger attached.
    /// </summary>
    [ProtoMember(4)]
    public bool IsRunningWithDebugger { get; init; }
}
