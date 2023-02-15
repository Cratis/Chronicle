// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents the information related to a connected client.
/// </summary>
/// <param name="ConnectionId">The unique connection id.</param>
/// <param name="ClientUri">The uri of the client.</param>
/// <param name="Version">Version of the client.</param>
/// <param name="LastSeen">The date and time the client was last seen.</param>
/// <param name="IsRunningWithDebugger">Whether or not the client is running with debugger attached.</param>
public record ConnectedClient(ConnectionId ConnectionId, Uri ClientUri, string Version, DateTimeOffset LastSeen, bool IsRunningWithDebugger);
