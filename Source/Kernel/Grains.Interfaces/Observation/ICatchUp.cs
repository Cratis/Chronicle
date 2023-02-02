// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a catch up process for an observer.
/// </summary>
public interface ICatchUp : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Starts a catch up process for a given observer.
    /// </summary>
    /// <param name="subscriberType">Type of subscriber.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(Type subscriberType);
}
