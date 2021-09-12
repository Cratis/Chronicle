// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents the sequence number within an event log.
    /// </summary>
    /// <param name="Value">Actual value.</param>
    public record EventLogSequenceNumber(uint Value) : ConceptAs<uint>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="uint"/> to <see cref="EventLogSequenceNumber"/>.
        /// </summary>
        /// <param name="sequenceNumber">Value to convert.</param>
        /// <returns>Converted <see cref="EventLogSequenceNumber"/>.</returns>
        public static implicit operator EventLogSequenceNumber(uint sequenceNumber) => new(sequenceNumber);
    }
}
