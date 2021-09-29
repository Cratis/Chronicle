// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.GraphQL
{
    /// <summary>
    /// Attribute to use for setting route for a containing class of queries or mutations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GraphRootAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GraphRootAttribute"/>
        /// </summary>
        /// <param name="path">Path to use</param>
        public GraphRootAttribute(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the path of the route
        /// </summary>
        public string Path { get; }
    }
}
