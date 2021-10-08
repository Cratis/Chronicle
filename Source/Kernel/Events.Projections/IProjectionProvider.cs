// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system that can provide <see cref="IProjection">projections</see>.
    /// </summary>
    public interface IProjectionProvider
    {
        /// <summary>
        /// Gets all projections provided.
        /// </summary>
        IProjection All { get; }
    }
}
