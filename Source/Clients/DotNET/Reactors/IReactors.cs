// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system for working with Reactor registrations for the Kernel.
/// </summary>
public interface IReactors
{
    /// <summary>
    /// Discover all Reactors from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all Reactors with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Gets a specific handler by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to get for.</param>
    /// <returns><see cref="ReactorHandler"/> instance.</returns>
    ReactorHandler GetHandlerById(ReactorId id);
}
