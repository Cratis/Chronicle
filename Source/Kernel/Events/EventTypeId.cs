// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events
{
    /// <summary>
    /// Represents the concept of the unique identifier of a type of event.
    /// </summary>@
    /// <param name="Value">Actual value.</param>
    public record EventTypeId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="EventTypeId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator EventTypeId(Guid id) => new(id);
    }
}
