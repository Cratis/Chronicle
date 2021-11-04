// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public string Path { get; }

        /// <summary>
        /// Gets the segments the full property path consists of.
        /// </summary>
        public string[] Segments { get; }
    }
}
