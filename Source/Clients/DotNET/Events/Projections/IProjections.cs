// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the system for working with projections.
    /// </summary>
    public interface IProjections
    {
        /// <summary>
        /// Start all projections.
        /// </summary>
        void StartAll();
    }
}
