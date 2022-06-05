// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Defines a system that can work with registering <see cref="IOutboxProjections"/>.
/// </summary>
public interface IOutboxProjectionsRegistrar
{
    /// <summary>
    /// Discover all and register all with the Kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegisterAll();
}
