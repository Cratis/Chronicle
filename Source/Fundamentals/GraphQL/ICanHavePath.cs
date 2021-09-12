// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.GraphQL
{
    /// <summary>
    /// Defines something that has a name.
    /// </summary>
    public interface ICanHavePath
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Check whether or not the name is set.
        /// </summary>
        bool HasPath { get; }
    }
}
