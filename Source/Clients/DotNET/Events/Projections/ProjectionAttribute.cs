// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Attribute used to adorn classes to define a projection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProjectionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionAttribute"/> class.
        /// </summary>
        /// <param name="identifier">Projection unique identifier.</param>
        public ProjectionAttribute(string identifier)
        {
            Identifier = identifier;
        }

        /// <summary>
        /// Gets the unique identifier for the target projection.
        /// </summary>
        public ProjectionId Identifier { get; }
    }
}
