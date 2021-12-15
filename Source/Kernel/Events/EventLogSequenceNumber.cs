// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events
{
    /// <summary>
    /// Represents the sequence number within an event log for an event.
    /// </summary>
    /// <param name="Value">The sequence number</param>
    public record EventLogSequenceNumber(uint Value): ConceptAs<uint>(Value)
    {
        /// <summary>
        /// Gets the first sequence number.
        /// </summary>
        public static readonly EventLogSequenceNumber First = 0u;

        /// <summary>
        /// Implicitly convert from <see cref="uint"/> to <see cref="EventLogSequenceNumber"/>.
        /// </summary>
        /// <param name="Value">Value to convert from.</param>
        /// <returns>A converted <see cref="EventLogSequenceNumber"/>.</returns>;
        public static implicit operator EventLogSequenceNumber(uint Value) => new (Value);
    }
}
