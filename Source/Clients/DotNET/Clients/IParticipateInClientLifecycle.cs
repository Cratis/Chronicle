// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

/// <summary>
/// Defines an interface for systems that want to participate in the lifecycle of the client.
/// </summary>
public interface IParticipateInClientLifecycle
{
    /// <summary>
    /// Called when the client gets connected to the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ClientConnected();

    /// <summary>
    /// Called when the client is disconnected to the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ClientDisconnected();
}
