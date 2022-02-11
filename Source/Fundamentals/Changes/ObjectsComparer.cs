// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents an implementation of <see cref="IObjectsComparer"/>.
    /// </summary>
    public class ObjectsComparer : IObjectsComparer
    {
        /// <inheritdoc/>
        public bool Compare<TTarget>(TTarget left, TTarget right, out IEnumerable<PropertyDifference<TTarget>> differences)
        {
            differences = Array.Empty<PropertyDifference<TTarget>>();
            return false;
        }
    }
}
