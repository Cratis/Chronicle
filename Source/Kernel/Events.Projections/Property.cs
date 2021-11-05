// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Strings;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an encapsulation of a property in the system - used for accessing properties on objects.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Property"/> class.
        /// </summary>
        /// <param name="path">Path to the property relative within an object.</param>
        public Property(string path)
        {
            Path = path;
            Segments = path.Split('.').Select(_ => _.ToCamelCase()).ToArray();
        }

        /// <summary>
        /// Gets the full path of the property.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the segments the full property path consists of.
        /// </summary>
        public string[] Segments { get; }

        /// <summary>
        /// Gets the last segment of the path.
        /// </summary>
        public string LastSegment => Segments[^1];

        /// <summary>
        /// Gets the value at the path of the property.
        /// </summary>
        /// <param name="expandoObject"><see cref="ExpandoObject"/> to get from.</param>
        /// <returns>Value, if any.</returns>
        public object? GetValue(ExpandoObject expandoObject)
        {
            var inner = expandoObject.MakeSurePathIsFulfilled(this) as IDictionary<string, object>;
            return inner.ContainsKey(LastSegment) ? inner[LastSegment] : null;
        }

        /// <summary>
        /// Set a specific value at the path of the property.
        /// </summary>
        /// <param name="expandoObject"><see cref="ExpandoObject"/> to set to.</param>
        /// <param name="value">Value to set.</param>
        public void SetValue(ExpandoObject expandoObject, object value)
        {
            var inner = expandoObject.MakeSurePathIsFulfilled(this) as IDictionary<string, object>;
            inner[LastSegment] = value;
        }
    }
}
