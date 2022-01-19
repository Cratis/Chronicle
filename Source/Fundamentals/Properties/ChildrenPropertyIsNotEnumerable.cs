// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Exception that gets thrown when a children property is not enumerable.
    /// </summary>
    public class ChildrenPropertyIsNotEnumerable : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildrenPropertyIsNotEnumerable"/> class.
        /// </summary>
        /// <param name="property"><see cref="PropertyPath"/> that is wrong type.</param>
        public ChildrenPropertyIsNotEnumerable(PropertyPath property) : base($"Property at '{property.Path}' is not of enumerable type.")
        {
        }
    }
}
