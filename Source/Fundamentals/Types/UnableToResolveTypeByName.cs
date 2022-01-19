// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Types
{
    /// <summary>
    /// Exception that gets thrown when a type is not possible to be resolved by its name.
    /// </summary>
    public class UnableToResolveTypeByName : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToResolveTypeByName"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type that was not possible to resolve.</param>
        public UnableToResolveTypeByName(string typeName)
            : base($"Unable to resolve '{typeName}'.")
        {
        }
    }
}
