// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Defines a system that discovers and registers the projections the kernel declares for itself.
/// </summary>
public interface IKernelProjections
{
    /// <summary>
    /// Discover every kernel-owned projection and register it for an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to register for.</param>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegister(EventStoreName eventStore);
}
