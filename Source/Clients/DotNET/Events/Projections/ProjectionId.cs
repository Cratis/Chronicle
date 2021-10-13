// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the unique identifier of a <see cref="IProjectionFor{TModel}"/>.
    /// </summary>
    /// <param name="Value">The value.</param>
    public record ProjectionId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="ProjectionId"/>.
        /// </summary>
        /// <param name="value"><see cref="Guid"/> to convert from.</param>
        public static implicit operator ProjectionId(Guid value) => new(value);

        /// <summary>
        /// Implicitly convert from string representation of a <see cref="Guid"/> to <see cref="ProjectionId"/>.
        /// </summary>
        /// <param name="value"><see cref="Guid"/> to convert from.</param>
        public static implicit operator ProjectionId(string value) => new(Guid.Parse(value));
    }
}
