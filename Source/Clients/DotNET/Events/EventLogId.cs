// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events
{
    /// <summary>
    /// Represents the unique identifier of a <see cref="IEventLog"/>.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record EventLogId(Guid Value) : ConceptAs<Guid>(Value)
    {
        /// <summary>
        /// The <see cref="EventLogId"/> representing the default <see cref="IEventLog"/>.
        /// </summary>
        public static readonly EventLogId Default = Guid.Empty;

        /// <summary>
        /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="EventLogId"/>.
        /// </summary>
        /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
        public static implicit operator EventLogId(string id) => new(Guid.Parse(id));

        /// <summary>
        /// Implicitly convert from <see cref="Guid"/> to <see cref="EventLogId"/>.
        /// </summary>
        /// <param name="id"><see cref="Guid"/> to convert from.</param>
        public static implicit operator EventLogId(Guid id) => new(id);
    }
}
