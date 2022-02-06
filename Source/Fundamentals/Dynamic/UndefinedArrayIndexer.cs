// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic
{
    /// <summary>
    /// Exception that gets thrown when an array indexer is not defined within a <see cref="PropertyPath"/>.
    /// </summary>
    public class UndefinedArrayIndexer : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UndefinedArrayIndexer"/> class.
        /// </summary>
        /// <param name="propertyPath"><see cref="PropertyPath"/>.</param>
        /// <param name="arrayIndex">The array indexer identifier.</param>
        public UndefinedArrayIndexer(PropertyPath propertyPath, string arrayIndex) : base($"Missing array indexer for '{arrayIndex}' in property path '{propertyPath}'")
        {
        }
    }
}
