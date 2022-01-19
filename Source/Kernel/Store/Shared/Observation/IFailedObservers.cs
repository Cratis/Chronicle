// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation
{
    /// <summary>
    /// Defines a system for working with failed observers.
    /// </summary>
    public interface IFailedObservers
    {
        /// <summary>
        /// Gets all failed observers.
        /// </summary>
        /// <returns>Collection of <see cref="FailedObserverState"/>.</returns>
        Task<IEnumerable<FailedObserverState>> GetAll();
    }
}
