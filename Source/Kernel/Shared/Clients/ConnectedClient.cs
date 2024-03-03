// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents the information related to a connected client.
/// </summary>
public class ConnectedClient
{
    /// <summary>
    /// Gets the unique connection id.
    /// </summary>
    public ConnectionId ConnectionId { get; init; } = ConnectionId.NotSet;

    /// <summary>
    /// Gets the version of the client.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time the client was last seen.
    /// </summary>
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>
    /// Gets whether or not the client is running with debugger attached.
    /// </summary>
    public bool IsRunningWithDebugger { get; init; }
}
