// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis
{
    /// <summary>
    /// Represents the concept of the microservice identifier.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record MicroserviceId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// The value when microservice identifier is not specified.
        /// </summary>
        public static readonly MicroserviceId Unspecified = new(Guid.Empty);

        /// <summary>
        /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="MicroserviceId"/>.
        /// </summary>
        /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
        public static implicit operator MicroserviceId(string id) => new(Guid.Parse(id));

        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="MicroserviceId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator MicroserviceId(Guid id) => new(id);
    }
}
