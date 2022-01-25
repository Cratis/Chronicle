// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store
{
    /// <summary>
    /// Represents the metadata related to an event.
    /// </summary>
    /// <param name="SequenceNumber">The <see cref="EventLogSequenceNumber"/>.</param>
    /// <param name="Type">The <see cref="EventType"/>.</param>
    public record EventMetadata(EventLogSequenceNumber SequenceNumber, EventType Type);
}
