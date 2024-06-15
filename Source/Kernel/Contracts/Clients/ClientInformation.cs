// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Represents the information sent to the Kernel when connecting.
/// </summary>
[ProtoContract]
public class ClientInformation
{
    /// <summary>
    /// Gets or sets the version of the client.
    /// </summary>
    [ProtoMember(1)]
    public string ClientVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether or not the client is running with debugger attached.
    /// </summary>
    [ProtoMember(2)]
    public bool IsRunningWithDebugger { get; set; }
}
