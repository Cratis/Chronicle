// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Attribute used to adorn classes to define a projection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AdapterAttribute : Attribute
    {
        /// <summary>
        /// Gets the unique identifier for the target projection.
        /// </summary>
        public AdapterId Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterAttribute"/> class.
        /// </summary>
        /// <param name="identifier">Projection unique identifier.</param>
        public AdapterAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}
