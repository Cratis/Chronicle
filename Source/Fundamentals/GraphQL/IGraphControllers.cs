// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.GraphQL
{
    /// <summary>
    /// Defines a system that knows about <see cref="GraphController"/> implementations.
    /// </summary>
    public interface IGraphControllers
    {
        /// <summary>
        /// Gets all <see cref="GraphController"/> types.
        /// </summary>
        IEnumerable<Type> All { get; }
    }
}
