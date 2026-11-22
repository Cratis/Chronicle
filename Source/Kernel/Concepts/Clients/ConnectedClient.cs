// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Clients;

/// <summary>
/// Represents the information related to a connected client.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Clients.ConnectedClient")]
public record ConnectedClient
{
    /// <summary>
    /// Gets the unique connection id.
    /// </summary>
    [Id(0)]
    public ConnectionId ConnectionId { get; init; } = ConnectionId.NotSet;

    /// <summary>
    /// Gets the version of the client.
    /// </summary>
    [Id(1)]
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time the client was last seen.
    /// </summary>
    [Id(2)]
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>
    /// Gets whether the client is running with debugger attached.
    /// </summary>
    [Id(3)]
    public bool IsRunningWithDebugger { get; init; }

    /// <summary>
    /// Gets the identifier of the client process.
    /// </summary>
    [Id(4)]
    public int ProcessId { get; init; }

    /// <summary>
    /// Gets the full path of the client process executable.
    /// </summary>
    [Id(5)]
    public string ProcessPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the machine the client process is running on.
    /// </summary>
    [Id(6)]
    public string MachineName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the client (for example, <c language="csharp">.NET</c>).
    /// </summary>
    [Id(7)]
    public string ClientType { get; init; } = string.Empty;
}
