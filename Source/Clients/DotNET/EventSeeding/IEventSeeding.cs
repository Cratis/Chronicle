// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSeeding;

/// <summary>
/// Defines the event seeding API surface.
/// </summary>
public interface IEventSeeding : IEventSeedingBuilder
{
    /// <summary>
    /// Discovers and invokes all registered event seeders.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndSeed(CancellationToken cancellationToken = default);
}
