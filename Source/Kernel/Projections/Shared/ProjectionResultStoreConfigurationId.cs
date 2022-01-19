// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents the unique identifier of a specific store configuration using in a projection pipeline.
    /// </summary>
    /// <param name="Value">Underlying value.</param>
    public record ProjectionResultStoreConfigurationId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="string"/> representation of <see cref="Guid"/> to <see cref="ProjectionResultStoreConfigurationId"/>.
        /// </summary>
        /// <param name="value"><see cref="string"/> representation of <see cref="Guid"/>.</param>
        /// <returns>Converted <see cref="ProjectionResultStoreConfigurationId"/> instance.</returns>
        public static implicit operator ProjectionResultStoreConfigurationId(string value) => new(Guid.Parse(value));
    }
}
