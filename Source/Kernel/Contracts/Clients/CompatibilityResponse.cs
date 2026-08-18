// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// The server's verdict on whether it still serves the contracts a client expects.
/// </summary>
/// <remarks>
/// Carries the server's own versions so the client can say which two things could not talk to each other without
/// making a second call - the whole point being that a client refusing to connect should say so in a way that names
/// what to change.
/// </remarks>
[ProtoContract]
public class CompatibilityResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the server still serves everything the client expects.
    /// </summary>
    [ProtoMember(1)]
    public bool IsCompatible { get; set; }

    /// <summary>
    /// Gets or sets the ways in which the server no longer serves the client, one sentence each.
    /// </summary>
    [ProtoMember(2)]
    public IEnumerable<string> Incompatibilities { get; set; } = [];

    /// <summary>
    /// Gets or sets the version of the running server.
    /// </summary>
    [ProtoMember(3)]
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the contracts the server serves - its protocol version.
    /// </summary>
    [ProtoMember(4)]
    public string ServerProtocolVersion { get; set; } = string.Empty;
}
