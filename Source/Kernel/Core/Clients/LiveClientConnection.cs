// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents a client connection that is open, and still responding, on the local silo.
/// </summary>
/// <param name="ConnectionId">The <see cref="ConnectionId"/> of the connection.</param>
/// <param name="Version">The version of the client.</param>
/// <param name="IsRunningWithDebugger">Whether the client is running with a debugger attached.</param>
/// <param name="ProcessId">The process identifier of the client.</param>
/// <param name="ProcessPath">The process path of the client.</param>
/// <param name="MachineName">The machine name of the client.</param>
/// <param name="ClientType">The type of the client.</param>
internal record LiveClientConnection(
    ConnectionId ConnectionId,
    string Version,
    bool IsRunningWithDebugger,
    int ProcessId,
    string ProcessPath,
    string MachineName,
    string ClientType);
