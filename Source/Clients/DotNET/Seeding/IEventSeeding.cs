// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines the event seeding API surface.
/// </summary>
public interface IEventSeeding : IEventSeedingBuilder
{
    /// <summary>
    /// Discovers all registered event seeders.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Registers all discovered event seeders by invoking them and sending seed data to the server.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();
}
