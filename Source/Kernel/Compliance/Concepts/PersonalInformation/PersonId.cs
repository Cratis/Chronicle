// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Compliance.Concepts.PersonalInformation
{
    /// <summary>
    /// Represents the concept of a unique identifier that identifies a person.
    /// </summary>
    /// <param name="Value">Underlying value.</param>
    public record PersonId(string Value) : ConceptAs<string>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="PersonId"/> to <see cref="EventSourceId"/>.
        /// </summary>
        /// <param name="personId"><see cref="PersonId"/> to convert from.</param>
        /// <returns>A new <see cref="EventSourceId"/>.</returns>.
        public static implicit operator EventSourceId(PersonId personId) => new(personId.Value);
    }
}
