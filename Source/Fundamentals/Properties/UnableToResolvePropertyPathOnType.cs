// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Exception that gets thrown when property path is not possible to be resolved on a type.
    /// </summary>
    public class UnableToResolvePropertyPathOnType : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToResolvePropertyPathOnType"/> class.
        /// </summary>
        /// <param name="type">Type that does not hold the property path.</param>
        /// <param name="path">The <see cref="PropertyPath"/> that is not possible to resolve.</param>
        public UnableToResolvePropertyPathOnType(Type type, PropertyPath path) : base($"Unable to resolve property path '${path}' on type '${type.FullName}'")
        {
        }
    }
}
