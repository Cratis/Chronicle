// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Concepts
{
    /// <summary>
    /// Exception that gets thrown when a <see cref="Type"/> is not a <see cref="ConceptAs{T}"/>.
    /// </summary>
    public class TypeIsNotAConcept : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeIsNotAConcept"/> class.
        /// </summary>
        /// <param name="type"><see cref="Type"/> that is not a concept.</param>
        public TypeIsNotAConcept(Type type)
            : base($"Type '{type.AssemblyQualifiedName}' is not a concept - implement ConceptAs<> for it to be one.")
        {
        }
    }
}
