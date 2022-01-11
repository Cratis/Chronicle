// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Defines a system for working with observers.
    /// </summary>
    public interface IObservers : IGrainWithGuidKey
    {
        /// <summary>
        /// Trigger a retry of all failed observers.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        Task RetryFailed();
    }
}
