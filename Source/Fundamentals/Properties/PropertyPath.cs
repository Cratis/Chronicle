// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Objects;
using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents an encapsulation of a property in the system - used for accessing properties on objects.
    /// </summary>
    public class PropertyPath
    {
        /// <summary>
        /// Implicitly convert from <see cref="PropertyPath"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="property"><see cref="PropertyPath"/> to convert from.</param>
        /// <returns>Converted path.</returns>
        public static implicit operator string(PropertyPath property) => property.Path;

        /// <summary>
        /// Implicitly convert from <see cref="string"/> to a <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="path">The path of the property.</param>
        /// <returns>Converted <see cref="PropertyPath"/>.</returns>
        public static implicit operator PropertyPath(string path) => new(path);

        /// <summary>
        /// Represents the root path.
        /// </summary>
        public static readonly PropertyPath Root = new(string.Empty);

        /// <summary>
        /// Gets the full path of the property.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the segments the full property path consists of.
        /// </summary>
        public IEnumerable<string> Segments => _segments;

        /// <summary>
        /// Gets the last segment of the path.
        /// </summary>
        public string LastSegment => _segments[^1];

        /// <summary>
        /// Gets whether or not this is the root path.
        /// </summary>
        public bool IsRoot => Path?.Length == 0;

        readonly string[] _segments;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPath"/> class.
        /// </summary>
        /// <param name="path">Path to the property relative within an object.</param>
        public PropertyPath(string path)
        {
            Path = path;
            _segments = path.Split('.').Select(_ => _.ToCamelCase()).ToArray();
        }

        /// <summary>
        /// Gets the value at the path of the property.
        /// </summary>
        /// <param name="target">Object to get from.</param>
        /// <returns>Value, if any.</returns>
        public object? GetValue(object target)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                var innerInstance = targetAsExpandoObject.EnsurePath(this) as IDictionary<string, object>;
                return innerInstance.ContainsKey(LastSegment) ? innerInstance[LastSegment] : null;
            }

            var inner = target.EnsurePath(this);
            var propertyInfo = GetPropertyInfoFor(target.GetType());
            return propertyInfo.GetValue(inner);
        }

        /// <summary>
        /// Set a specific value at the path of the property.
        /// </summary>
        /// <param name="target">Object to set to.</param>
        /// <param name="value">Value to set.</param>
        public void SetValue(object target, object value)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                var inner = targetAsExpandoObject.EnsurePath(this) as IDictionary<string, object>;
                inner[LastSegment] = value;
            }
            else
            {
                var inner = target.EnsurePath(this);
                var propertyInfo = GetPropertyInfoFor(target.GetType());
                propertyInfo.SetValue(inner, value);
            }
        }

        /// <summary>
        /// Get the corresponding <see cref="PropertyInfo"/> for the full path from the root type.
        /// </summary>
        /// <typeparam name="TRoot">Type of root.</typeparam>
        /// <returns>The <see cref="PropertyInfo"/>.</returns>
        /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve the property path on the type.</exception>
        public PropertyInfo GetPropertyInfoFor<TRoot>() => GetPropertyInfoFor(typeof(TRoot));

        /// <summary>
        /// Get the corresponding <see cref="PropertyInfo"/> for the full path from the root type.
        /// </summary>
        /// <param name="rootType">Type of root.</param>
        /// <returns>The <see cref="PropertyInfo"/>.</returns>
        /// <exception cref="UnableToResolvePropertyPathOnType">Thrown if not able to resolve the property path on the type.</exception>
        public PropertyInfo GetPropertyInfoFor(Type rootType)
        {
            var currentType = rootType;
            PropertyInfo? currentPropertyInfo = null;

            foreach (var segment in Segments)
            {
                currentPropertyInfo = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
            }

            if (currentPropertyInfo is null)
            {
                throw new UnableToResolvePropertyPathOnType(currentType, this);
            }

            return currentPropertyInfo;
        }

        /// <inheritdoc/>
        public override string ToString() => Path;
    }
}
