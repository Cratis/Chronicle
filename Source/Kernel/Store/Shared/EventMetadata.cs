// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Represents the metadata related to an event.
    /// </summary>
    /// <param name="SequenceNumber">The <see cref="EventLogSequenceNumber"/>.</param>
    /// <param name="EventType">The <see cref="EventType"/>.</param>
    public record EventMetadata(EventLogSequenceNumber SequenceNumber, EventType EventType);
}
