// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

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
            var allDifferences = new List<PropertyDifference<TTarget>>();
            differences = allDifferences;

            var currentPropertyPath = new PropertyPath(string.Empty);

            foreach (var property in typeof(TTarget).GetProperties())
            {
                var leftValue = property.GetValue(left);
                var rightValue = property.GetValue(right);

                var propertyPath = currentPropertyPath + property.Name;

                if (!leftValue!.Equals(rightValue))
                {
                    allDifferences.Add(new PropertyDifference<TTarget>(propertyPath, left, right));
                }
            }

            return allDifferences.Count > 0;
        }
    }
}
