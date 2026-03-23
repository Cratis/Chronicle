// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Host;

/// <summary>
/// Defines the contract for the server.
/// </summary>
[Service]
public interface IServer
{
    /// <summary>
    /// Instruct the server to reload its state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ReloadState();

    /// <summary>
    /// Gets version information about the running server instance.
    /// </summary>
    /// <returns>A <see cref="ServerVersionInfo"/> with server and contracts versions.</returns>
    [Operation]
    Task<ServerVersionInfo> GetVersionInfo();
}
