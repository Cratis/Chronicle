// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation
{
    /// <summary>
    /// Concept that represents the unique identifier of an observer.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record ObserverId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// Gets the representation of an unspecified <see cref="EventSourceId"/>.
        /// </summary>
        public static readonly ObserverId Unspecified = new(Guid.Empty);

        /// <summary>
        /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="ObserverId"/>.
        /// </summary>
        /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
        public static implicit operator ObserverId(string id) => new(Guid.Parse(id));

        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="ObserverId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator ObserverId(Guid id) => new(id);
    }
}
