// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a catch up process for an observer.
/// </summary>
public interface ICatchUp : IGrainWithGuidKey
{
    /// <summary>
    /// Starts a catch up process for a given observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Start();
}
