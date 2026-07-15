// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

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
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether or not the client is running with debugger attached.
    /// </summary>
    [ProtoMember(3)]
    public bool IsRunningWithDebugger { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the client process.
    /// </summary>
    [ProtoMember(4)]
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the full path of the client process executable.
    /// </summary>
    [ProtoMember(5)]
    public string ProcessPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the machine the client process is running on.
    /// </summary>
    [ProtoMember(6)]
    public string MachineName { get; set; } = string.Empty;
}
