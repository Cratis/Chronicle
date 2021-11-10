// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Properties;
using Newtonsoft.Json;

namespace Cratis.Objects
{
    /// <summary>
    /// Extension methods for any object.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Deep clone an object.
        /// </summary>
        /// <param name="source">Object clone.</param>
        /// <typeparam name="T">Type of object to clone.</typeparam>
        /// <returns>Cloned instance.</returns>
        public static T Clone<T>(this T source)
        {
            var sourceAsString = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(sourceAsString)!;
        }

        /// <summary>
        /// Ensure a <see cref="PropertyPath"/> on an object.
        /// </summary>
        /// <param name="source">Object to ensure on.</param>
        /// <param name="propertyPath"><see cref="PropertyPath"/> to ensure.</param>
        /// <returns>Instance of the last segment in the path.</returns>
        /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve parts of the property path on the type.</exception>
        public static object EnsurePath(this object source, PropertyPath propertyPath)
        {
            var currentType = source.GetType();
            var currentInstance = source;

            for (var segmentIndex = 0; segmentIndex < propertyPath.Segments.Length - 1; segmentIndex++)
            {
                var segment = propertyPath.Segments[segmentIndex];
                var currentPropertyInfo = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);

                if (currentPropertyInfo is null)
                {
                    throw new UnableToResolvePropertyPathOnType(currentType, propertyPath);
                }

                currentInstance = currentPropertyInfo.GetValue(currentInstance);
                if (currentInstance is null)
                {
                    var newInstance = Activator.CreateInstance(currentPropertyInfo.PropertyType);
                    currentPropertyInfo.SetValue(currentInstance, newInstance);
                    currentInstance = newInstance;
                }
            }

            return currentInstance!;
        }
    }
}
