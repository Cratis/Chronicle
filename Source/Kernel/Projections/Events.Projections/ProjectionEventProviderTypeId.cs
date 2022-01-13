// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the unique identifier of a type of <see cref="IProjectionEventProvider"/>.
    /// </summary>
    /// <param name="Value">Underlying value.</param>
    public record ProjectionEventProviderTypeId(Guid Value) : ConceptAs<Guid>(Value)
    {

        /// <summary>
        /// Implicitly convert from <see cref="string"/> representation of a <see cref="Guid"/> to <see cref="ProjectionEventProviderTypeId"/>.
        /// </summary>
        /// <param name="value">Guid as string</param>
        /// <returns>A new <see cref="ProjectionEventProviderTypeId"/></returns>
        public static implicit operator ProjectionEventProviderTypeId(string value) => new(Guid.Parse(value));
    }
}
