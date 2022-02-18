// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Defines a system working with <see cref="ArrayIndexer"/>.
    /// </summary>
    public interface IArrayIndexers
    {
        /// <summary>
        /// Get an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
        /// <returns>The <see cref="ArrayIndexer"/>.</returns>
        ArrayIndexer GetFor(PropertyPath propertyPath);

        /// <summary>
        /// Check if there is an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="propertyPath"><see cref="PropertyPath"/> to check for.</param>
        /// <returns>True if it has it, false if not.</returns>
        bool HasFor(PropertyPath propertyPath);
    }
}
