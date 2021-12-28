// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events
{
    /// <summary>
    /// Represents the unique identifier of an instance of an event source.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record EventSourceId(string Value) : ConceptAs<string>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="EventSourceId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
        public static implicit operator EventSourceId(Guid id) => new(id.ToString());

        /// <summary>
        /// Implicitly convert from <see cref="string"/> to <see cref="EventSourceId"/>.
        /// </summary>
        /// <param name="id"><see cref="string"/> to convert from.</param>
        /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
        public static implicit operator EventSourceId(string id) => new(id);
    }
}
