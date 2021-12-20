// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events
{
    /// <summary>
    /// Represents the concept of an event source unique identifier.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record EventSourceId(string Value) : ConceptAs<string>(Value)
    {
        /// <summary>
        /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="EventSourceId"/>.
        /// </summary>
        /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
        public static implicit operator EventSourceId(string id) => new(Guid.Parse(id));

        /// <summary>
        /// /// Implicitly convert from <see cref="Guid"/> to <see cref="EventSourceId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator EventSourceId(Guid id) => new(id);
    }
}
