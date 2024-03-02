// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a system for managing <see cref="IObserver">observers</see>.
/// </summary>
public interface IObservers : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Rehydrate all observers.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();
}
