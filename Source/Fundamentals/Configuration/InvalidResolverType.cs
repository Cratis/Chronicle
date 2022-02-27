// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration
{
    /// <summary>
    /// Exception that gets thrown when a resolver does not implement <see cref="IConfigurationValueResolver"/>.
    /// </summary>
    public class InvalidResolverType : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidResolverType"/> class.
        /// </summary>
        /// <param name="type">The invalid type.</param>
        public InvalidResolverType(Type type) : base($"The type '{type.FullName}' does not implement the `{typeof(IConfigurationValueResolver).FullName}`")
        {
        }
    }
}
