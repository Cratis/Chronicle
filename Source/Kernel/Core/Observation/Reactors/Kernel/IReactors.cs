// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Defines a system for managing kernel reactors.
/// </summary>
public interface IReactors
{
    /// <summary>
    /// Discovers and registers all available kernel reactors.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegister(EventStoreName eventStore, EventStoreNamespaceName namespaceName);
}
