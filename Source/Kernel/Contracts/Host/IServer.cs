// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

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
}
