// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Extension methods for working with <see cref="ExpandoObject"/>.
    /// </summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// Get or create instance at a specific <see cref="Property"/>.
        /// </summary>
        /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
        /// <param name="property"><see cref="Property"/> to get or create for.</param>
        /// <returns><see cref="ExpandoObject"/> at property.</returns>
        public static ExpandoObject MakeSurePathIsFulfilled(this ExpandoObject target, Property property)
        {
            var currentTarget = target as IDictionary<string, object>;
            for (var propertyIndex = 0; propertyIndex < property.Segments.Length - 1; propertyIndex++)
            {
                var segment = property.Segments[propertyIndex];
                if (!currentTarget.ContainsKey(segment))
                {
                    var nested = new ExpandoObject();
                    currentTarget[segment] = nested;
                    currentTarget = nested!;
                }
                else
                {
                    currentTarget = ((ExpandoObject)currentTarget[segment])!;
                }
            }

            return (currentTarget as ExpandoObject)!;
        }
    }
}
