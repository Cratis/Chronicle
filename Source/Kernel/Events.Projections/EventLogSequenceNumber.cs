// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the sequence number within an event log for an event.
    /// </summary>
    /// <param name="Value">The sequence number</param>
    public record EventLogSequenceNumber(uint Value): ConceptAs<uint>(Value);
}
