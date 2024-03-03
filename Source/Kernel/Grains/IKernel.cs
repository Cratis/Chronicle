// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage;

namespace Cratis.Kernel.Grains;

/// <summary>
/// Defines the entrypoint for the Kernel.
/// </summary>
public interface IKernel
{
    /// <summary>
    /// Gets the <see cref="IStorage"/> for the kernel.
    /// </summary>
    IStorage Storage { get; }

    /// <summary>
    /// Get an <see cref="IEventStore"/> for a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="name"><see cref="EventStoreName"/> to get for.</param>
    /// <returns><see cref="IEventStore"/> instance.</returns>
    IEventStore GetEventStore(EventStoreName name);
}
