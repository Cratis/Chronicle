// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Exception that gets thrown when a <see cref="ArrayIndexer"/> is missing for a <see cref="PropertyPath"/>.
    /// </summary>
    public class MissingArrayIndexerForPropertyPath : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingArrayIndexerForPropertyPath"/> class.
        /// </summary>
        /// <param name="propertyPath"><see cref="PropertyPath"/> it is missing for.</param>
        public MissingArrayIndexerForPropertyPath(PropertyPath propertyPath) : base($"Missing array indexer for '{propertyPath}'")
        {
        }
    }
}
