// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Types
{
    /// <summary>
    /// Exception that gets thrown when multiple types are found and not allowed.
    /// </summary>
    public class MultipleTypesFound : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleTypesFound"/> class.
        /// </summary>
        /// <param name="type">Type that multiple of it.</param>
        /// <param name="typesFound">The types that was found.</param>
        public MultipleTypesFound(Type type, IEnumerable<Type> typesFound)
            : base($"More than one type found for '{type.FullName}' - types found : [{string.Join(",", typesFound.Select(_ => _.FullName))}]")
        {
        }
    }
}
