// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents an implementation of <see cref="IArrayIndexers"/>.
    /// </summary>
    public class ArrayIndexers : IArrayIndexers
    {
        readonly IDictionary<PropertyPath, ArrayIndexer> _arrayIndexers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayIndexers"/> class.
        /// </summary>
        /// <param name="arrayIndexers">A collection of <see cref="ArrayIndexer">array indexers</see>.</param>
        public ArrayIndexers(IEnumerable<ArrayIndexer> arrayIndexers)
        {
            _arrayIndexers = arrayIndexers.ToDictionary(_ => _.ArrayProperty, _ => _);
        }

        /// <inheritdoc/>
        public ArrayIndexer GetFor(PropertyPath propertyPath)
        {
            ThrowIfMissingArrayIndexerForPropertyPath(propertyPath);
            return _arrayIndexers[propertyPath];
        }

        /// <inheritdoc/>
        public bool HasFor(PropertyPath propertyPath) => _arrayIndexers.ContainsKey(propertyPath);

        void ThrowIfMissingArrayIndexerForPropertyPath(PropertyPath propertyPath)
        {
            if (!_arrayIndexers.ContainsKey(propertyPath))
            {
                throw new MissingArrayIndexerForPropertyPath(propertyPath);
            }
        }
    }
}
