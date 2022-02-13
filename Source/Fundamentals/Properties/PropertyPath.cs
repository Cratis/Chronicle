// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        /// Operator overload for equality comparison.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if they're equal, false if not.</returns>
        public static bool operator ==(PropertyPath left, PropertyPath right) => left.Equals(right);

        /// <summary>
        /// Operator overload for not-equality comparison.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if they're not equal, false if they are.</returns>
        public static bool operator !=(PropertyPath left, PropertyPath right) => !left.Equals(right);

        /// <summary>
        /// Adds two <see cref="PropertyPath"/> together - formatting it correctly.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>A merged <see cref="PropertyPath"/>.</returns>
        public static PropertyPath operator +(PropertyPath left, PropertyPath right)
        {
            var builder = new StringBuilder();
            if (left.Path.Length > 0)
            {
                builder.Append(left.Path);
            }

            if (left.Path.Length > 0 && right.Path.Length > 0)
            {
                builder.Append('.');
            }

            if (right.Path.Length > 0)
            {
                builder.Append(right.Path);
            }

            return new(builder.ToString());
        }

        /// <summary>
        /// Add a <see cref="IPropertyPathSegment"/> to a <see cref="PropertyPath"/> and return a new instance.
        /// </summary>
        /// <param name="left"><see cref="PropertyPath"/> to add to.</param>
        /// <param name="segment"><see cref="IPropertyPathSegment"/> to add.</param>
        /// <returns>New <see cref="PropertyPath"/>.</returns>
        public static PropertyPath operator +(PropertyPath left, IPropertyPathSegment segment)
        {
            return segment switch
            {
                PropertyName => left.AddProperty(segment.Value),
                ArrayProperty => left.AddArrayIndex(segment.Value),
                _ => left
            };
        }

        /// <summary>
        /// Represents the root path.
        /// </summary>
        public static readonly PropertyPath Root = new(string.Empty);

        static Regex? _arrayIndexRegex;
        readonly IPropertyPathSegment[] _segments = Array.Empty<IPropertyPathSegment>();

        /// <summary>
        /// Gets the full path of the property.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the segments the full property path consists of.
        /// </summary>
        public IEnumerable<IPropertyPathSegment> Segments => _segments;

        /// <summary>
        /// Gets the last segment of the path.
        /// </summary>
        public IPropertyPathSegment LastSegment => _segments[^1];

        /// <summary>
        /// Gets whether or not this is the root path.
        /// </summary>
        public bool IsRoot => Path?.Length == 0;

        static Regex ArrayIndexRegex => _arrayIndexRegex ??= new("\\[(?<property>[\\w-_]*)\\]", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPath"/> class.
        /// </summary>
        /// <param name="path">Path to the property relative within an object.</param>
        public PropertyPath(string path)
        {
            _segments = path.Split('.').Select<string, IPropertyPathSegment>(_ =>
            {
                var match = ArrayIndexRegex!.Match(_);
                if (match.Success)
                {
                    return new ArrayProperty(match.Groups["property"].Value.ToCamelCase());
                }
                return new PropertyName(_.ToCamelCase());
            }).ToArray();

            Path = string.Join('.', (IEnumerable<object>)_segments);
        }

        /// <summary>
        /// Add an <see cref="PropertyName"/> as segment by creating a new <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <returns>A new <see cref="PropertyPath"/> with the segment appended.</returns>
        /// <remarks>This operation does not mutate the original.</remarks>
        public PropertyPath AddProperty(string name)
        {
            if (Path.Length == 0)
            {
                return new(name);
            }
            return new($"{Path}.{name}");
        }

        /// <summary>
        /// Add an <see cref="ArrayProperty"/> as segment by creating a new <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="identifier">Identifier of the array segment.</param>
        /// <returns>A new <see cref="PropertyPath"/> with the segment appended.</returns>
        /// <remarks>This operation does not mutate the original.</remarks>
        public PropertyPath AddArrayIndex(string identifier)
        {
            if (Path.Length == 0)
            {
                return new($"[{identifier}]");
            }
            return new($"{Path}.[{identifier}]");
        }

        /// <summary>
        /// Check whether or not there is a value at the path of the property for a specific target.
        /// </summary>
        /// <param name="target">Object to get from.</param>
        /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
        /// <returns>Value, if any.</returns>
        public bool HasValue(object target, IEnumerable<ArrayIndexer> arrayIndexers)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                var innerInstance = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
                return innerInstance.ContainsKey(LastSegment.Value);
            }

            var inner = target.EnsurePath(this, arrayIndexers);
            var propertyInfo = GetPropertyInfoFor(target.GetType());
            return propertyInfo.GetValue(inner) != null;
        }

        /// <summary>
        /// Gets the value at the path of the property.
        /// </summary>
        /// <param name="target">Object to get from.</param>
        /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
        /// <returns>Value, if any.</returns>
        public object? GetValue(object target, IEnumerable<ArrayIndexer> arrayIndexers)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                var innerInstance = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
                return innerInstance.ContainsKey(LastSegment.Value) ? innerInstance[LastSegment.Value] : null;
            }

            var inner = target.EnsurePath(this, arrayIndexers);
            var propertyInfo = GetPropertyInfoFor(target.GetType());
            return propertyInfo.GetValue(inner);
        }

        /// <summary>
        /// Set a specific value at the path of the property.
        /// </summary>
        /// <param name="target">Object to set to.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
        public void SetValue(object target, object value, IEnumerable<ArrayIndexer> arrayIndexers)
        {
            if (target is ExpandoObject targetAsExpandoObject)
            {
                var inner = targetAsExpandoObject.EnsurePath(this, arrayIndexers) as IDictionary<string, object>;
                inner[LastSegment.Value] = value;
            }
            else
            {
                var inner = target.EnsurePath(this, arrayIndexers);
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
                currentPropertyInfo =
                    currentType.GetProperty(segment.Value, BindingFlags.Public | BindingFlags.Instance) ??
                    currentType.GetProperty(segment.Value.ToPascalCase(), BindingFlags.Public | BindingFlags.Instance);
            }

            if (currentPropertyInfo is null)
            {
                throw new UnableToResolvePropertyPathOnType(currentType, this);
            }

            return currentPropertyInfo;
        }

        /// <inheritdoc/>
        public override string ToString() => Path;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj as PropertyPath)?.Path.Equals(Path, StringComparison.InvariantCulture) ?? false;

        /// <inheritdoc/>
        public override int GetHashCode() => Path.GetHashCode(StringComparison.InvariantCulture);
    }
}
