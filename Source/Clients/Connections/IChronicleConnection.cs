// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a system that manages the connection to Cratis.
/// </summary>
public interface IChronicleConnection : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IConnectionLifecycle"/> service.
    /// </summary>
    IConnectionLifecycle Lifecycle { get; }

    /// <summary>
    /// Connects to the Chronicle Kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connect();

    /// <summary>
    /// Ask the connection to drop its session and connect again, which gives the client a new connection id and
    /// re-registers everything with the kernel. Used when the kernel reports that it no longer knows this client.
    /// </summary>
    void RequestReconnect()
    {
    }
}
