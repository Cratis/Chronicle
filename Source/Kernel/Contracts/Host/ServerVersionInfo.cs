// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Host;

/// <summary>
/// Represents version information returned by the server.
/// </summary>
[ProtoContract]
public class ServerVersionInfo
{
    /// <summary>
    /// Gets or sets the server version (e.g. "1.2.3").
    /// </summary>
    [ProtoMember(1)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contracts assembly version. CLI and server are compatible
    /// when their contracts versions share the same major version.
    /// </summary>
    [ProtoMember(2)]
    public string ContractsVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commit SHA the server was built from, if available.
    /// </summary>
    [ProtoMember(3)]
    public string CommitSha { get; set; } = string.Empty;
}
