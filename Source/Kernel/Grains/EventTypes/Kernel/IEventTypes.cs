// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.EventTypes.Kernel;

/// <summary>
/// Represents the event types in the system.
/// </summary>
public interface IEventTypes
{
    /// <summary>
    /// Discovers and registers the event types for the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAndRegister();
}
