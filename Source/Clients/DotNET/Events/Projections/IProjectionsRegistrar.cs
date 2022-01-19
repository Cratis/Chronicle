// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines the system for working with projections.
    /// </summary>
    public interface IProjectionsRegistrar
    {
        /// <summary>
        /// Start all projections.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        Task StartAll();
    }
}
