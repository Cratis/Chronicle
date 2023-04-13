// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Silos;

/// <summary>
/// Represents a scavenger that collects dead silos.
/// </summary>
public interface IDeadSilosScavenger : IGrainWithGuidKey
{
    /// <summary>
    /// Start the scavenger.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Start();
}
